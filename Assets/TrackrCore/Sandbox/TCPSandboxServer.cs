using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using SimpleJSON;
using UnityEngine;

public class TCPSandboxServer : MonoBehaviour
{
    #region private members 	
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
    /// Create handle to connected tcp client. 	
    /// </summary> 	
    //private TcpClient connectedTcpClient;

    //private Dictionary<byte, TcpClient> peers;
    private ConcurrentDictionary<byte, TcpClient> peers;

    #endregion

    public ushort port = 8052;
    public byte id;

    // Use this for initialization
    void Start()
    {
        peers = new ConcurrentDictionary<byte, TcpClient>();
        // Start TcpServer background thread 		
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();

        //peerListenerThread = new Thread(new ThreadStart(ListenForPeerMessages));
        //peerListenerThread.IsBackground = true;
        //peerListenerThread.Start();

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            SendMessage("");

        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("disconnect");
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("fire");
        }

    }

    private void Connect(byte id, TcpClient client) {
        Debug.Log("connecting to " + id);
        peers[id] = client;
    }

    private void Disconnect(byte id) {
        Debug.Log("disconnect " + id);
        peers[id].Close();
        TcpClient tmp;
        if (!peers.TryRemove(id, out tmp)) {
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

        using (NetworkStream stream = client.GetStream())
        {
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
                    Connect(id, client);
                    //Debug.Log("connecting to " + id);
                    //peers[id] = client;
                }

                Debug.Log("client message received as: " + clientMessage);

            }
        }


        //});
        //childSocketThread.IsBackground = true;
        //childSocketThread.Start();
    }
    /// <summary> 	
    /// Runs in background TcpServerThread; Handles incomming TcpClient requests 	
    /// </summary> 	
    private void ListenForIncommingRequests()
    {
        try
        {
            // Create listener on localhost port 8052. 			
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            
            Debug.Log("Server is listening");
            
            while (true)
            {
                TcpClient connectedTcpClient = tcpListener.AcceptTcpClient();//)
                                                                   //{
                Debug.Log("Accepted new client");
                //new Thread(new ThreadStart(ThreadProc));
                var thread = new Thread(() => ThreadProc(connectedTcpClient));
                thread.Start();
                //connectedTcpClient.Client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.KeepAlive, true);

                
               // ThreadPool.QueueUserWorkItem(ThreadProc, connectedTcpClient);
                
                    
                //}
                
            }
        }
        catch (SocketException socketException) {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }
    /// <summary> 	
    /// Send message to client using socket connection. 	
    /// </summary> 	
    private void SendMessage(string message)
    {
        /*
        if (connectedTcpClient == null)
        {
            return;
        }
        */
        try
        {
            foreach (byte peerID in peers.Keys)
            {
                //Debug.Log(peer.Key + ": " + peer.Value);
                // Get a stream object for writing. 			
                NetworkStream stream = peers[peerID].GetStream();
                
                //Debug.Log("fire");

                if (stream.CanWrite)
                {
                    //string serverMessage = "This is a message from your server.";
                    // Convert string message to byte array.                 
                    byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(message);
                    // Write byte array to socketConnection stream.               
                    stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                    Debug.Log("Server sent his message - should be received by client");
                } else {
                    Debug.Log("Client#" + peerID + ": Can't write");
                }
                
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }
}