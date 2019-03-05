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

public class UDPSandboxPeer : TrackerGroup
{
    #region private members
    private Thread clientReceiveThread;

    /// <summary> 	
    /// TCPListener to listen for incomming TCP connection 	
    /// requests. 	
    /// </summary> 	
    //private TcpListener tcpListener;
    //private UdpClient udpListener;
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
    public VRClient peerClient;

    //public ConcurrentDictionary<byte, NetworkStream> peerStreams;
    //public ushort port = 0;
    //public ushort clientPort = 8053;
    public ConcurrentQueue<KeyValuePair<byte, string>> messageQueue;

    public NetworkManager network;
    public NetworkStatistics statistics;

    //public int heartbeatPeriod = 10000;

    // Use this for initialization
    public new void Start()
    {
        base.Start();
        if (network.NetworkConfiguration == null)
        {
            network.Start();
        }

        if (id == network.id)
        {
            enabled = false;
            return;
        }

        //peers = new ConcurrentDictionary<byte, TcpClient>();
        //peerPorts = new ConcurrentDictionary<byte, ushort>();
        peerClient = new VRClient();
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
        active = true;
        ConnectToUdpServer();
    }

    // Update is called once per frame
    public new void Update()
    {
        base.Update();
        if (peerClient.connected)
        {
            // TODO: implement color changing on connection
            //TrackerMesh.GetComponent<Renderer>().material.color = Color.green;
        }
        else
        {
            // TODO: implement color changing on connection
            //TrackerMesh.GetComponent<Renderer>().material.color = Color.white;
        }
        if (peerClient == null || !tracked)
        {
            return;
        }
        // send over tracker position in camera frame
        // this is the vector from our client to their client
        JSONNode info = JSON.Parse("{}");
        info["diff"] = JSON.Parse("{}");
        Vector3 framePos = obsPos - Frame.Pose.position;
        info["diff"]["x"] = framePos.x;
        info["diff"]["y"] = framePos.y;
        info["diff"]["z"] = framePos.z;
        SendData(info, "POSE_OTHER");

        // if we receive data from them, we can now figure out the positional and rotational difference
        if (peerClient.connected)
        {
            // p12 is the vector from client 1 (us) to client 2 (them) in our local frame
            // p21 is the vector from client 2 to client 1 in their local frame
            // p1, p2, and q2 is the local pose data of each client in their own frame
            // Note: most of these project onto the xz plane because y/up-angle is the only angle that drifts
            Vector3 p12 = framePos;
            Vector3 p21 = peerClient.relPos;
            Quaternion q2 = peerClient.rot;
            Vector3 p1 = Frame.Pose.position;
            Vector3 p2 = peerClient.pos;
            p12 = Vector3.Normalize(Vector3.ProjectOnPlane(p12, Vector3.up));
            p21 = Vector3.Normalize(Vector3.ProjectOnPlane(p21, Vector3.up));
            // forward vectors
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
            diff_alpha = 1f;// Mathf.Max(diff_alpha - 0.05f, 0.1f);

            // apply final result to virtual tracker
            transform.rotation = Quaternion.identity;
            transform.Rotate(Vector3.up, rot_diff);
            transform.rotation = transform.rotation * q2;


            // update the network manager so that other objects in scene can reference it
            peerClient.rot_diff = rot_diff;
            peerClient.pos_diff = pos_diff;

            //Debug.Log(string.Format("UNITY: a1: {0}, a2: {1}, a: {2}, alpha: {3}", a1, a2, a, diff_alpha));
        }

        if (!messageQueue.IsEmpty)
        {
            foreach (KeyValuePair<byte, string> kv in messageQueue)
            {

                SendMessage(kv.Value);
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

        // TODO: move to global manager

        //Broadcast(Visible_M.ToString(id));

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

        statistics.tickReceived(root["type"].Value);

        if (root["type"].Value == "CONNECT")
        {
            //Connect(peerID, client);
            //Debug.Log("connecting to " + id);
            //peers[id] = client;

            connectionPublisher.PublishData(root);

            //Broadcast(Connect_ACK_M.ToString(id));

        }
        else if (root["type"].Value == "CONNECT_ACK")
        {

            connectionAckPublisher.PublishData(root);

        }
        else if (root["type"].Value == "DISCONNECT")
        {

            Disconnect(id);

        }
        else if (root["type"].Value == "SPANNINGTREE")
        {
            //n[i], parent = recv(nbr i)
            //var id = root["id"];
            //var p = root["info"]["parent"];

            // TODO: move to global manager
            //var d = peerClients[FindClosestPeer()].latency;
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
        else if (root["type"].Value == "POSE_SELF")
        {
            Quaternion rot = new Quaternion(root["info"]["rot"]["x"].AsFloat,
                                         root["info"]["rot"]["y"].AsFloat,
                                         root["info"]["rot"]["z"].AsFloat,
                                         root["info"]["rot"]["w"].AsFloat);
            Vector3 pos = new Vector3(root["info"]["pos"]["x"].AsFloat,
                                      root["info"]["pos"]["y"].AsFloat,
                                      root["info"]["pos"]["z"].AsFloat);
            peerClient.SetRot(rot);
            peerClient.SetPos(pos);
        }
        else if (root["type"].Value == "POSE_OTHER")
        {
            Vector3 relpos = new Vector3(root["info"]["diff"]["x"].AsFloat,
                                         root["info"]["diff"]["y"].AsFloat,
                                         root["info"]["diff"]["z"].AsFloat);
            peerClient.SetRelPos(relpos);
        }
        else if (root["type"].Value == "POSE_OBJECT")
        {
            byte objId = (byte)root["info"]["id"].AsInt;
            Quaternion rot = new Quaternion(root["info"]["rot"]["x"].AsFloat,
                                         root["info"]["rot"]["y"].AsFloat,
                                         root["info"]["rot"]["z"].AsFloat,
                                         root["info"]["rot"]["w"].AsFloat);
            Vector3 pos = new Vector3(root["info"]["pos"]["x"].AsFloat,
                                      root["info"]["pos"]["y"].AsFloat,
                                      root["info"]["pos"]["z"].AsFloat);
            Pose p = new Pose
            {
                pos = pos,
                rot = rot
            };
            peerClient.objects[objId] = p;

        }
    }

    public void Connect(byte id, UdpClient client)
    {
        Debug.Log("connecting to " + id);
        peerClient.client = client;
    }

    public void Disconnect(byte id)
    {
        Debug.Log("disconnect " + id);
        peerClient.client.Close();
    }

    /// <summary> 	
    /// Setup socket connection. 	
    /// </summary> 	
    private void ConnectToUdpServer()
    {
        try
        {
            clientReceiveThread = new Thread((() => ListenForData()));
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
    private void ListenForData()
    {
        try
        {
            peerClient.port = network.NetworkConfiguration[id].port;
            peerClient.IP = network.NetworkConfiguration[id].hostname;
            peerClient.client = new UdpClient();
            peerClient.client.Client.ExclusiveAddressUse = false;
            peerClient.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(network.GetHostname()), peerClient.port);
            peerClient.client.Client.Bind(ep);
            peerClient.connected = true;
            DataListen();
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    /// <summary> 	
    /// Send message to client using socket connection. 	
    /// </summary> 	
    public void SendMessage(string message)
    {
        // obviously stupid to allow sending messages to yourself in this context
        if (id == network.id)
        {
            return;
        }

        try
        {
            Byte[] sendbuf = System.Text.Encoding.UTF8.GetBytes(message);
            IPEndPoint sendEP = new IPEndPoint(IPAddress.Parse(peerClient.IP), network.GetPort());
            peerClient.client.Send(sendbuf, sendbuf.Length, sendEP);
            //Debug.Log("UNITY: sending message to " + id + ": " + message);
        }
        catch (Exception ex)
        {
            if (ex is SocketException)
            {
                Debug.Log("Socket exception " + id + "@" + id + ": " + ex);
                return;
            }
            else if (ex is InvalidOperationException)
            {
                Debug.Log("InvalidOperationException " + id + "@" + id + ": " + ex);
                return;
            }

            throw;

        }

    }

    public void SendData(JSONNode data, string type)
    {
        JSONNode root = JSON.Parse("{}");
        root["id"] = network.id;
        root["type"] = type;
        root["info"] = data;

        statistics.tickSent(root["type"].Value);
        
        root["packetID"] = statistics.lastPacketID++;

        string message = root.ToString();
        SendMessage(message);
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
        if (peerID == id)
        {
            return;
        }

        try
        {

            peerClient.latency = PingHost(peerClient.IP);

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
        IPEndPoint ep = new IPEndPoint(IPAddress.Parse(network.GetHostname()), peerClient.port);
        UdpState state = new UdpState(ep, peerClient.client);
        peerClient.client.BeginReceive(new AsyncCallback(ReceiveCallback), state);
        while (active)
        {
            Thread.Sleep(100);
        }
        // active is set to false when the application terminates, then this thread can cleanup
        Debug.Log("closing connection...");
        peerClient.client.Close();
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
        //Debug.Log("UNITY: received message " + clientMessage);
        OnMessage(clientMessage);
        // loop the callback
        UdpState state = new UdpState(e, c);
        c.BeginReceive(new AsyncCallback(ReceiveCallback), state);
    }
}
