using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using GoogleARCore;

using System.Net.NetworkInformation;

using SimpleJSON;
using UnityEngine;
using Ping = System.Net.NetworkInformation.Ping;

public class UDPSandboxPeer : Tracker
{
    #region private members
    private Thread clientReceiveThread;

    /// <summary> 	
    /// TCPListener to listen for incomming TCP connection 	
    /// requests. 	
    /// </summary> 	
    //private TcpListener tcpListener;
    private UdpClient udpListener;
    /// <summary> 
    /// Background thread for TcpServer workload. 	
    /// </summary> 	
    private Thread udpListenerThread;

    private Thread peerListenerThread;
    /// <summary> 	
    /// Create handle to connected tcp clients.
    /// </summary>


    private IPublisher<JSONNode> connectionPublisher;
    private Subscriber<JSONNode> connectionSubscriber;
    private IPublisher<JSONNode> connectionAckPublisher;
    private Subscriber<JSONNode> connectionAckSubscriber;

    private IPublisher<ElapsedEventArgs> heartbeatPublisher;
    private Subscriber<ElapsedEventArgs> heartbeatSubscriber;

    IPEndPoint ep;
    bool active = false;

    private float rot_diff = 0f;
    private Vector3 pos_diff = Vector3.zero;
    private float diff_alpha = 1f;

    #endregion
    public Heartbeat heartbeat;
    //public ConcurrentDictionary<byte, long> peerLatencies;
    //public ConcurrentDictionary<byte, string> peerAddresses;
    //public ConcurrentDictionary<byte, TcpClient> peers;
    //public ConcurrentDictionary<byte, ushort> peerPorts;
    public ConcurrentDictionary<byte, VRClient> peerClients;

    //public ConcurrentDictionary<byte, NetworkStream> peerStreams;
    //public ushort port = 0;
    //public ushort clientPort = 8053;
    public ConcurrentQueue<KeyValuePair<byte, string>> messageQueue;

    public NetworkManager network;

    //public int heartbeatPeriod = 10000;

    // Use this for initialization
    public new void Start()
    {
        if (network.NetworkConfiguration == null)
        {
            network.Start();
        }
        // Unnecessary to run the code, but useful for getting host IP
        // on Android, where we can't run ipconfig
        string hostName = Dns.GetHostName();
        foreach (IPAddress addr in Dns.GetHostEntry(hostName).AddressList)
        {
            Debug.Log("UNITY: IP Address: " + addr.ToString());
        }

        //peers = new ConcurrentDictionary<byte, TcpClient>();
        //peerPorts = new ConcurrentDictionary<byte, ushort>();
        peerClients = new ConcurrentDictionary<byte, VRClient>();
        messageQueue = new ConcurrentQueue<KeyValuePair<byte, string>>();

        connectionPublisher = new Publisher<JSONNode>();
        connectionSubscriber = new Subscriber<JSONNode>(connectionPublisher);
        connectionSubscriber.Publisher.DataPublisher += OnConnection;

        connectionAckPublisher = new Publisher<JSONNode>();
        connectionAckSubscriber = new Subscriber<JSONNode>(connectionAckPublisher);
        connectionAckSubscriber.Publisher.DataPublisher += OnConnectionACK;

        //heartbeat = new Heartbeat(2000, 1.0f);
        //heartbeat.DataPublisher += OnHeartbeat;
        //heartbeat.resetTimer();
        //heartbeat.aTimer.Elapsed += OnHeartbeat;

        ConnectToUdpServer(id);

        // these are used to maintain asynchronous data receiving
        active = true;

        ep = new IPEndPoint(IPAddress.Parse(network.GetHostname()), network.GetPort());
        udpListener = new UdpClient();
        udpListener.ExclusiveAddressUse = false;
        udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        Debug.Log("opening connection...");
        Debug.Log(ep.Address.ToString() + ", " + ep.Port);
        udpListener.Client.Bind(ep);


        // start async receive
        udpListenerThread = new Thread(new ThreadStart(DataListen));
        udpListenerThread.IsBackground = true;
        udpListenerThread.Start();

        //peerListenerThread = new Thread(new ThreadStart(ListenForPeerMessages));
        //peerListenerThread.IsBackground = true;
        //peerListenerThread.Start();

    }

    // Update is called once per frame
    public new void Update()
    {
        base.Update();
        if (peerClients.ContainsKey(id) && peerClients[id].connected)
        {
            TrackerMesh.GetComponent<Renderer>().material.color = Color.green;
        }
        else
        {
            TrackerMesh.GetComponent<Renderer>().material.color = Color.white;
        }
        if (!peerClients.ContainsKey(id) || !tracked)
        {
            return;
        }
        // send over tracker position in camera frame
        // this is the vector from our client to their client
        JSONNode info = JSON.Parse("{}");
        info["diff"] = JSON.Parse("{}");
        info["diff"]["x"] = rawPos.x;
        info["diff"]["y"] = rawPos.y;
        info["diff"]["z"] = rawPos.z;
        Debug.Log("UNITY: sending tracker data");
        SendData(info, "POSE_OTHER", id);

        // if we receive data from them, we can now figure out the positional and rotational difference
        if (peerClients[id].connected)
        {
            // p12 is the vector from client 1 (us) to client 2 (them) in our local frame
            // p21 is the vector from client 2 to client 1 in their local frame
            // p1, p2, and q2 is the local pose data of each client in their own frame
            // Note: most of these project onto the xz plane because y/up-angle is the only angle that drifts
            Vector3 p12 = rawPos;
            Vector3 p21 = peerClients[id].relPos;
            Quaternion q2 = peerClients[id].rot;
            Vector3 p1 = Frame.Pose.position;
            Vector3 p2 = peerClients[id].pos;
            p12 = Vector3.Normalize(Vector3.ProjectOnPlane(p12, Vector3.up));
            p21 = Vector3.Normalize(Vector3.ProjectOnPlane(p21, Vector3.up));
            // forward vectors of 
            Vector3 f1 = Vector3.ProjectOnPlane(Frame.Pose.forward, Vector3.up);
            Vector3 f2 = Vector3.ProjectOnPlane(q2 * Vector3.forward, Vector3.up);
            // a1, a2 are the angles between the forward vector of a client and the vector from the client to the other client
            // we use these angles to find the final correction angle
            float a1 = Vector3.SignedAngle(p12, f1, Vector3.up);
            float a2 = Vector3.SignedAngle(p21, f2, Vector3.up);

            float a = 180f - (a1 - a2);
            // adjust a to account for the rotations already made locally within the system
            a += Vector3.SignedAngle(Vector3.forward, f1, Vector3.up) - Vector3.SignedAngle(Vector3.forward, f2, Vector3.up);

            // finally, use a LPF/complemetary filter to smooth angle/position deltas
            // IDEALLY deltas in rot_diff should be small/0, but in practice this isn't true due to innate CV errors
            // finding a good alpha for the complementary filter is critical in making this all work!
            rot_diff = Mathf.LerpAngle(rot_diff, a, diff_alpha);
            Vector3 p = transform.position - (Quaternion.Euler(0f, rot_diff, 0f) * p2);
            pos_diff = Vector3.Lerp(pos_diff, p, diff_alpha);
            diff_alpha = Mathf.Max(diff_alpha - 0.05f, 0.1f);

            // apply final result to virtual tracker
            transform.rotation = Quaternion.identity;
            transform.Rotate(Vector3.up, rot_diff);
            transform.rotation = transform.rotation * q2;


            // update the network manager so that other objects in scene can reference it
            peerClients[id].rot_diff = rot_diff;
            peerClients[id].pos_diff = pos_diff;

            //Debug.Log(string.Format("UNITY: a1: {0}, a2: {1}, a: {2}", a1, a2, a));
        }

        if (!messageQueue.IsEmpty)
        {
            foreach (KeyValuePair<byte, string> kv in messageQueue)
            {

                SendMessage(kv.Key, kv.Value);
                KeyValuePair<byte, string> result;
                if (!messageQueue.TryDequeue(out result))
                {
                    Debug.Log("failed to dequeue");
                }
            }
        }
        /*
        if (Input.GetKeyDown(KeyCode.C)) {
            Broadcast(Connect_M.ToString(id));
        }

        if (Input.GetKeyDown(KeyCode.D)) {
            Debug.Log("disconnect");
        }

        if (Input.GetKeyDown(KeyCode.H)) {
            Debug.Log("fire");
        }
        */
    }


    void OnConnection(object sender, MessageArgument<JSONNode> e)
    {
        Debug.Log("OnConnection: " + e.Message.ToString());

        byte peerID = (byte)Convert.ToUInt16(e.Message["id"].ToString());
        messageQueue.Enqueue(new KeyValuePair<byte, string>(peerID, Connect_ACK_M.ToString(peerID)));
        //Thread t = new Thread(() => SendMessage(peerID, Connect_ACK_M.ToString(id)));
        //SendMessage(peerID, Connect_ACK_M.ToString(id));
        //t.Start();
        //t.Join();

    }

    void OnConnectionACK(object sender, MessageArgument<JSONNode> e)
    {
        Debug.Log("OnConnectionACK: " + e.Message.ToString());

    }

    void OnDisconnection()
    {

    }

    void OnDisconnectionACK()
    {

    }

    void OnHeartbeat(object sender, ElapsedEventArgs e)
    {

        Broadcast(Visible_M.ToString(id));

        //messageQueue.Enqueue(new KeyValuePair<byte, string>(peerID, Connect_ACK_M.ToString(peerID)));

    }

    void OnRedirect()
    {

    }

    void OnTimeout()
    {

    }

    void OnLatencyQuery()
    {

    }

    void OnTimeSync()
    {

    }

    void OnNotification()
    {

    }

    void OnMessage(string message)
    {
        var root = JSON.Parse(message);
        byte peerID = (byte)root["id"].AsInt;

        Debug.Log(message);

        if (root["type"] == "CONNECT")
        {
            //Connect(peerID, client);
            //Debug.Log("connecting to " + id);
            //peers[id] = client;

            connectionPublisher.PublishData(root);

            //Broadcast(Connect_ACK_M.ToString(id));

        }
        else if (root["type"] == "CONNECT_ACK")
        {

            connectionAckPublisher.PublishData(root);

        }
        else if (root["type"] == "DISCONNECT")
        {

            Disconnect(peerID);

        }
        else if (root["type"] == "SPANNINGTREE")
        {
            //n[i], parent = recv(nbr i)
            var id = root["id"];
            var p = root["info"]["parent"];

            var d = peerClients[FindClosestPeer()].latency;
            //var d = min(n) + 1;
            //d = min(n) + 1;

            /*
            parent = find(i s.t.n[i] = d - 1);

            send(parent, < d = d, parent = true >);

            for n in nbr {
                if n != parent {
                    send(n, < d = d, parent = false >)
                }
            }
            */
        }
    }

    public void Connect(byte id, UdpClient client)
    {
        Debug.Log("connecting to " + id);
        peerClients[id].client = client;
    }

    public void Disconnect(byte id)
    {
        Debug.Log("disconnect " + id);
        peerClients[id].client.Close();

        // TODO: check - is this still right?
        VRClient tmp;
        if (!peerClients.TryRemove(id, out tmp))
        {
            Debug.Log("failed to disconnect");
        }
    }

    private void ThreadProc(TcpClient client)
    {
        //var client = (TcpClient)obj;
        // var childSocketThread = new Thread(() =>
        // {
        // Get a stream object for reading 					
        NetworkStream stream = client.GetStream();

        // Read incomming stream into byte arrary. 						
        while (true)
        {

            //{
            Byte[] bytes = new Byte[1024];
            int length;

            if ((length = stream.Read(bytes, 0, bytes.Length)) != 0) {
                var incomingData = new byte[length];
                Array.Copy(bytes, 0, incomingData, 0, length);
                // Convert byte array to string message. 							
                string clientMessage = Encoding.ASCII.GetString(bytes);

                var root = JSON.Parse(clientMessage);
                byte peerID = (byte)root["id"].AsInt;
                if (root["type"] == "CONNECT")
                {
                    //Connect(peerID, client);
                    //Debug.Log("connecting to " + id);
                    //peers[id] = client;

                    connectionPublisher.PublishData(root);

                    //Broadcast(Connect_ACK_M.ToString(id));

                }
                else if (root["type"] == "CONNECT_ACK")
                {

                    connectionAckPublisher.PublishData(root);

                }
                else if (root["type"] == "DISCONNECT")
                {

                    Disconnect(peerID);

                }
                else if (root["type"] == "SPANNINGTREE")
                {
                    //n[i], parent = recv(nbr i)
                    var id = root["id"];
                    var p = root["info"]["parent"];

                    var d = peerClients[FindClosestPeer()].latency;
                    //var d = min(n) + 1;
                    //d = min(n) + 1;

                    /*
                    parent = find(i s.t.n[i] = d - 1);

                    send(parent, < d = d, parent = true >);
                    
                    for n in nbr {
                        if n != parent {
                            send(n, < d = d, parent = false >)
                        }
                    }
                    */
                }
                else if (root["type"] == "POSE_SELF")
                {
                    Quaternion rot = new Quaternion(root["info"]["rot"]["x"].AsFloat,
                                                 root["info"]["rot"]["y"].AsFloat,
                                                 root["info"]["rot"]["z"].AsFloat,
                                                 root["info"]["rot"]["w"].AsFloat);
                    Vector3 pos = new Vector3(root["info"]["pos"]["x"].AsFloat,
                                              root["info"]["pos"]["y"].AsFloat,
                                              root["info"]["pos"]["z"].AsFloat);
                    peerClients[id].SetRot(rot);
                    peerClients[id].SetPos(pos);
                }
                else if (root["type"] == "POSE_OTHER")
                {
                    Vector3 relpos = new Vector3(root["info"]["diff"]["x"].AsFloat,
                                                 root["info"]["diff"]["y"].AsFloat,
                                                 root["info"]["diff"]["z"].AsFloat);
                    peerClients[id].SetRelPos(relpos);
                }
                else if (root["type"] == "POSE_OBJECT")
                {
                    byte objId = (byte)root["info"]["id"].AsInt;
                    Quaternion rot = new Quaternion(root["info"]["rot"]["x"].AsFloat,
                                                 root["info"]["rot"]["y"].AsFloat,
                                                 root["info"]["rot"]["z"].AsFloat,
                                                 root["info"]["rot"]["w"].AsFloat);
                    Vector3 pos = new Vector3(root["info"]["pos"]["x"].AsFloat,
                                              root["info"]["pos"]["y"].AsFloat,
                                              root["info"]["pos"]["z"].AsFloat);
                    Pose p = new Pose();
                    p.pos = pos;
                    p.rot = rot;
                    peerClients[id].objects[objId] = p;

                }

                Debug.Log("UNITY: " + id + ":client message received as: " + clientMessage);
                //stream = client.GetStream();
            }

        }
        //}


        //});
        //childSocketThread.IsBackground = true;
        //childSocketThread.Start();
    }

    /// <summary> 	
    /// Setup socket connection. 	
    /// </summary> 	
    private void ConnectToUdpServer(byte _id)
    {
        try
        {
            clientReceiveThread = new Thread((() => ListenForData(_id)));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
        }
    }
    /// <summary> 	
    /// Runs in background clientReceiveThread; Listens for incoming data. 	
    /// </summary>     
    private void ListenForData(byte _id)
    {
        try
        {
            VRClient c = new VRClient();
            c.client = new UdpClient();
            c.port = network.NetworkConfiguration[_id].port;
            c.IP = network.NetworkConfiguration[_id].hostname;
            c.connected = true;
            peerClients[_id] = c;
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    /// <summary> 	
    /// Send message to client using socket connection. 	
    /// </summary> 	
    public void SendMessage(byte peerID, string message)
    {
        Debug.Log("UNITY: sending client " + peerID + " message " + message);
        // obviously stupid to allow sending messages to yourself in this context
        if (peerID == network.id)
        {
            return;
        }

        try
        {
            Debug.Log("UNITY: talk to " + peerID);

            Byte[] sendbuf = System.Text.Encoding.UTF8.GetBytes(message);
            IPEndPoint sendEP = new IPEndPoint(IPAddress.Parse(peerClients[peerID].IP), peerClients[peerID].port);
            peerClients[peerID].client.Send(sendbuf, sendbuf.Length);

            //Debug.Log("Peer " + id + " sent: " + message);

        }
        catch (Exception ex)
        {
            if (ex is SocketException)
            {
                Debug.Log("Socket exception " + id + "@" + peerID + ": " + ex);
                return;
            }
            else if (ex is InvalidOperationException)
            {
                Debug.Log("InvalidOperationException " + id + "@" + peerID + ": " + ex);
                return;
            }

            throw;

        }

    }

    public void SendData(JSONNode data, string type, byte sendid)
    {
        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = type;
        root["info"] = data;
        // TODO: last packet id
        //lastPacketID++;
        //root["packetID"] = lastPacketID;
        string message = root.ToString();
        SendMessage(sendid, message);
    }

    public void Broadcast(string message)
    {
        Debug.Log("Peer " + id + " broadcasting: " + message);
        foreach (byte peerID in peerClients.Keys)
        {
            SendMessage(peerID, message);
        }

    }

    public void BroadcastData(JSONNode data, string type)
    {
        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = type;
        root["info"] = data;
        // TODO: last packet id
        //lastPacketID++;
        //root["packetID"] = lastPacketID;
        string message = root.ToString();
        Broadcast(message);
    }

    byte FindClosestPeer()
    {

        byte minPeer = 0xFF;
        long minDistance = long.MaxValue;

        foreach (byte peerID in peerClients.Keys)
        {
            if (peerClients[peerID].latency < minDistance)
            {
                minPeer = peerID;
                minDistance = peerClients[peerID].latency;
            }
        }

        return minPeer;

    }

    // based on answer @ https://stackoverflow.com/questions/11800958/using-ping-in-c-sharp
    static long PingHost(string nameOrAddress)
    {
        //bool pingable = false;
        Ping pinger = null;
        long latency = -1;

        try
        {
            pinger = new Ping();
            PingReply reply = pinger.Send(nameOrAddress);
            //pingable = reply.Status == IPStatus.Success;
            if (reply.Status == IPStatus.Success)
            {
                latency = reply.RoundtripTime;
            }
            
        }
        catch (PingException)
        {
            // Discard PingExceptions and return false;
        }
        finally
        {
            if (pinger != null)
            {
                pinger.Dispose();
            }
        }

        return latency;
    }

    void Ping(byte peerID)
    {
        // obviously stupid to allow ping yourself in this context
        if (peerID == id) {
            return;
        }

        try {

            peerClients[peerID].latency = PingHost(peerClients[peerID].IP);

        } catch (Exception ex) {
            if (ex is SocketException) {
                Debug.Log("Socket exception " + id + "@" + peerID + ": " + ex);
                return;
            } else if (ex is InvalidOperationException) {
                Debug.Log("InvalidOperationException " + id + "@" + peerID + ": " + ex);
                return;
            }

            throw;
        }
    }

    void PingAll() {

        foreach (byte peerID in peerClients.Keys) {
            try {

                Ping(peerID);

            } catch (Exception ex) {
                if (ex is SocketException) {
                    Debug.Log("Socket exception " + id + "@" + peerID + ": " + ex);
                    return;
                } else if (ex is InvalidOperationException) {
                    Debug.Log("InvalidOperationException " + id + "@" + peerID + ": " + ex);
                    return;
                }

                throw;
            }
        }
    }

    /*
    void timesync()
    {
        int latency = currentTime - sentTime;
        int synchonizationDelta = currentTime - serverTime + (latency / 2);
        int syncedTime = now + synchonizationDelta;
    }
    */

    void OnSpanningTree()
    {

    }

    void MST()
    {
        foreach (byte peerID in peerClients.Keys)
        {

        }
    }

    // simple class for network status, passed in the async thread
    class UdpState : System.Object
    {
        public UdpState(IPEndPoint e, UdpClient c) { this.e = e; this.c = c; }
        public IPEndPoint e;
        public UdpClient c;
    }

    // begins the async receive loop
    void DataListen()
    {

        UdpState state = new UdpState(ep, udpListener);
        udpListener.BeginReceive(new AsyncCallback(ReceiveCallback), state);
        while (active)
        {
            Thread.Sleep(100);
        }
        // active is set to false when the application terminates, then this thread can cleanup
        Debug.Log("closing connection...");
        udpListener.Close();
        //foreach (VRClient c in Clients.Values)
        //{
        //if (c.client != null)
        //{
        //c.client.Close();
        //}
        //}
    }

    // async callback method
    void ReceiveCallback(IAsyncResult ar)
    {
        if (!active)
        {
            return;
        }
        UdpClient c = (UdpClient)((UdpState)(ar.AsyncState)).c;
        IPEndPoint e = (IPEndPoint)((UdpState)(ar.AsyncState)).e;
        // get data
        Byte[] recvbuf = c.EndReceive(ar, ref e);
        // parse data
        string clientMessage = System.Text.Encoding.UTF8.GetString(recvbuf);

        OnMessage(clientMessage);

        // loop the callback
        UdpState state = new UdpState(e, c);
        c.BeginReceive(new AsyncCallback(ReceiveCallback), state);
    }
}