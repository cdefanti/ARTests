using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

using System.Net.NetworkInformation;

using SimpleJSON;
using UnityEngine;
using Ping = System.Net.NetworkInformation.Ping;

public class UDPSandboxPeer : MonoBehaviour
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
    public byte id = 0;
    public ConcurrentQueue<KeyValuePair<byte, string>> messageQueue;

    // TODO: probably a better way to sync these up
    public byte[] KnownIDs;
    public string[] KnownHosts;
    public ushort[] KnownPorts;
    // maybe this should be a map to a custom struct instead of a tuple, makes it more readable later
    public Dictionary<byte, NetworkConfig> NetworkConfiguration;

    //public int heartbeatPeriod = 10000;

    // Use this for initialization
    void Start()
    {
        // Unnecessary to run the code, but useful for getting host IP
        // on Android, where we can't run ipconfig
        string hostName = Dns.GetHostName();
        foreach (IPAddress addr in Dns.GetHostEntry(hostName).AddressList)
        {
            Debug.Log("UNITY: IP Address: " + addr.ToString());
        }

        NetworkConfiguration = new Dictionary<byte, NetworkConfig>();
        // TODO: check for KnownX length mismatch.
        for (int i = 0; i < KnownIDs.Length; i++)
        {
            NetworkConfiguration[KnownIDs[i]] = new NetworkConfig(KnownHosts[i], KnownPorts[i]);
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

        // these are used to maintain asynchronous data receiving
        active = true;

        ep = new IPEndPoint(IPAddress.Parse(NetworkConfiguration[id].hostname), NetworkConfiguration[id].port);

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

        foreach (byte _id in NetworkConfiguration.Keys)
        {
            if (_id != id)
            {
                ConnectToUdpServer(_id);
            }
        }

        //peerListenerThread = new Thread(new ThreadStart(ListenForPeerMessages));
        //peerListenerThread.IsBackground = true;
        //peerListenerThread.Start();

    }

    // Update is called once per frame
    void Update()
    {

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
    private void ConnectToUdpServer(byte id)
    {
        try
        {
            clientReceiveThread = new Thread((() => ListenForData(id)));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
        }
    }
    /// <summary> 	
    /// Runs in background clientReceiveThread; Listens for incomming data. 	
    /// </summary>     
    private void ListenForData(byte _id)
    {
        try
        {
            VRClient c = new VRClient();
            c.client = new UdpClient();
            c.port = NetworkConfiguration[_id].port;
            c.IP = NetworkConfiguration[_id].hostname;
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
        // obviously stupid to allow sending messages to yourself in this context
        if (peerID == id)
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