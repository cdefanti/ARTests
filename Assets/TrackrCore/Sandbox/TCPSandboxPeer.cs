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

public class TCPSandboxPeer : MonoBehaviour
{
    #region private members 	
    private TcpClient socketConnection;
    private Thread clientReceiveThread;

    /// <summary> 	
    /// TCPListener to listen for incomming TCP connection 	
    /// requests. 	
    /// </summary> 	
    private TcpListener tcpListener;
    /// <summary> 
    /// Background thread for TcpServer workload. 	
    /// </summary> 	
    private Thread tcpListenerThread;

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

    #endregion
    public Heartbeat heartbeat;
    public ConcurrentDictionary<byte, long> peerLatencies;
    public ConcurrentDictionary<byte, string> peerAddresses;
    public ConcurrentDictionary<byte, TcpClient> peers;
    public ConcurrentDictionary<byte, ushort> peerPorts;
    public ConcurrentDictionary<byte, VRClient> peerClients;

    //public ConcurrentDictionary<byte, NetworkStream> peerStreams;
    public ushort port = 8052;
    //public ushort clientPort = 8053;
    public byte id = 0;
    public ConcurrentQueue<KeyValuePair<byte, string>> messageQueue;

    //public int heartbeatPeriod = 10000;

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

    // Use this for initialization
    void Start()
    {
        peers = new ConcurrentDictionary<byte, TcpClient>();
        peerPorts = new ConcurrentDictionary<byte, ushort>();
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

        // Start TcpServer background thread 		
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncomingRequests));
        tcpListenerThread.IsBackground = true;


        tcpListenerThread.Start();

        if (id == 0)
        {
            ConnectToTcpServer(8053);
            ConnectToTcpServer(8054);
        }
        else if (id == 1)
        {
            ConnectToTcpServer(8052);
            ConnectToTcpServer(8054);
        }
        else if (id == 2)
        {
            ConnectToTcpServer(8052);
            ConnectToTcpServer(8053);
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

    public void Connect(byte id, TcpClient client)
    {
        Debug.Log("connecting to " + id);
        peers[id] = client;
    }

    public void Disconnect(byte id)
    {
        Debug.Log("disconnect " + id);
        peers[id].Close();
        TcpClient tmp;
        if (!peers.TryRemove(id, out tmp))
        {
            Debug.Log("failed to disconnect");
        }
    }
    /*
    private void ListenForPeerMessages()
    {

        while (true)
        {
            
            foreach (KeyValuePair<byte, TcpClient> peer in peers)
            {

                NetworkStream stream = peer.Value.GetStream();
                
                Byte[] bytes = new Byte[1024];
                int length;

                // Read incomming stream into byte arrary. 						
                while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    var incomingData = new byte[length];
                    Array.Copy(bytes, 0, incomingData, 0, length);
                    // Convert byte array to string message. 							
                    string clientMessage = Encoding.ASCII.GetString(bytes);

                    var root = JSON.Parse(clientMessage);
                    byte id = (byte)root["id"].AsInt;
                    if (root["type"] == "CONNECT")
                    {
                        //Debug.Log("connecting to " + id);
                        //peers[id] = connectedTcpClient;
                    }

                    Debug.Log("...client message received as: " + clientMessage);

                }
                
            }
        }
        
    }
    */

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

                    var d = peerLatencies[FindClosestPeer()];
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

                Debug.Log(id + ":client message received as: " + clientMessage);
                //stream = client.GetStream();
            }

        }
        //}


        //});
        //childSocketThread.IsBackground = true;
        //childSocketThread.Start();
    }
    /// <summary> 	
    /// Runs in background TcpServerThread; Handles incomming TcpClient requests 	
    /// </summary> 	
    private void ListenForIncomingRequests()
    {
        try
        {
            // Create listener on localhost port 8052. 			
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            tcpListener.Start();

            Debug.Log(id + "Server is listening");

            while (true)
            {
                TcpClient connectedTcpClient = tcpListener.AcceptTcpClient();//)
                                                                             //{

                Debug.Log(id + "Accepted new client");
                //new Thread(new ThreadStart(ThreadProc));
                var thread = new Thread(() => ThreadProc(connectedTcpClient));
                thread.IsBackground = false;
                thread.Start();
                //connectedTcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.KeepAlive, true);


                // ThreadPool.QueueUserWorkItem(ThreadProc, connectedTcpClient);


                //}

            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    /// <summary> 	
    /// Setup socket connection. 	
    /// </summary> 	
    private void ConnectToTcpServer(ushort peerPort)
    {
        try
        {
            clientReceiveThread = new Thread((() => ListenForData(peerPort)));
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
    private void ListenForData(ushort peerPort)
    {
        try
        {
            if (peerPort == 8052)
            {
                socketConnection = peers[0] = new TcpClient("localhost", peerPort);
                peerPorts[0] = 8052;
                peerAddresses[0] = "localhost";
            }
            else if (peerPort == 8053)
            {
                socketConnection = peers[1] = new TcpClient("localhost", peerPort);
                peerPorts[1] = 8053;
                peerAddresses[1] = "localhost";
            }
            else if (peerPort == 8054)
            {
                socketConnection = peers[2] = new TcpClient("localhost", peerPort);
                peerPorts[2] = 8054;
                peerAddresses[2] = "localhost";
            }

            Byte[] bytes = new Byte[4096];

            while (true)
            {
                // Get a stream object for reading 				
                NetworkStream stream = socketConnection.GetStream();

                int length;
                // Read incomming stream into byte arrary. 					
                while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    var incomingData = new byte[length];
                    Array.Copy(bytes, 0, incomingData, 0, length);
                    // Convert byte array to string message.		
                    string serverMessage = Encoding.ASCII.GetString(incomingData);
                    Debug.Log(System.DateTime.UtcNow + ":server message received as: " + serverMessage);
                }
                Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            }
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
        if (peerID == id) {
            return;
        }

        try
        {
            //Debug.Log("talk to " + peerID);
            // Get a stream object for writing.
            NetworkStream stream = peers[peerID].GetStream();

            if (stream.CanWrite)
            {
                //string serverMessage = "This is a message from your server.";
                // Convert string message to byte array.                 
                byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(message);
                // Write byte array to socketConnection stream.               
                stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                Debug.Log("Server sent his message - should be received by client: " + message);
            }
            else
            {
                Debug.Log("Client#" + peerID + ": Can't write");
            }
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

        foreach (byte peerID in peers.Keys)
        {
            try
            {
                //Debug.Log("talk to " + peerID);
                // Get a stream object for writing.
                NetworkStream stream = peers[peerID].GetStream();

                if (stream.CanWrite)
                {
                    //string serverMessage = "This is a message from your server.";
                    // Convert string message to byte array.                 
                    byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(message);
                    // Write byte array to socketConnection stream.               
                    stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                    //  Debug.Log("Server sent his message - should be received by client");
                }
                else
                {
                    Debug.Log("Client#" + peerID + ": Can't write");
                }
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

        foreach (byte peerID in peers.Keys)
        {
            if (peerLatencies[peerID] < minDistance)
            {
                minPeer = peerID;
                minDistance = peerLatencies[peerID];
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

            peerLatencies[peerID] = PingHost(peerAddresses[peerID]);

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

        foreach (byte peerID in peers.Keys) {
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
        foreach (byte peerID in peers.Keys)
        {

        }
    }
}