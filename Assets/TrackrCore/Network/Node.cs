using System.Collections;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

using System.Threading;

using SimpleJSON;
using UnityEngine;

// basic concept of a network node encapsulated, abstract away the details from DDC, and abstract DDC away from peers
public class Node {
    // maintain connections to reference other peers
    Dictionary<byte, Connection> peers;

    const int bufferSize = 100;

    protected ushort port = 20000;
    protected byte id;
    protected string hostname;

    TcpListener listener;

    public Node(byte id, string hostname, ushort port) {
        this.id = id;
        this.hostname = hostname;
        this.port = port;

        peers = new Dictionary<byte, Connection>();
        //IPAddress ipAddress = IPAddress.Parse(hostname);

        Debug.Log("Starting TCP listener..." + IPAddress.Any + ":" + port);
        var listenerThread = new Thread(() =>
        {
            listener = new TcpListener(IPAddress.Any, port);
            eventLoop();
        });

        listenerThread.Start();
    }

    void eventLoop() {

        listener.Start();

        while (true) {

            Socket client = listener.AcceptSocket();
            Debug.Log("Connection accepted.");

            var childSocketThread = new Thread(() =>
            {
                byte[] recvbuf = new byte[512];
                int size = client.Receive(recvbuf);
                Debug.Log("Recieved data: " + size + " bytes");
                string data = System.Text.Encoding.UTF8.GetString(recvbuf);
                Debug.Log(JSON.Parse(data));

                Debug.Log("");

                client.Close();
            });

            childSocketThread.IsBackground = true;
            childSocketThread.Start();
        }

    }

    public bool connect(byte u, string hostname) {

        //peers[u] = new Connection(hostname, port);

        peers[u] = new Connection(this.hostname, this.port);
        //peers[u].connect(hostname, port);

        peers[u].sendCommand(Connect_M.ToString(id));
        Debug.Log("receiving..." + peers[u].receiveCommand());
        

        return true;

    }

    protected void sendCommand(byte id, string message) {

        peers[id].sendCommand(message);

    }

    protected void broadcastCommand(string message) {

        foreach (Connection connection in peers.Values) {
            connection.sendCommand(message);
        }

    }
    /*
    protected void sendData(byte id, string message) {

        peers[id].sendData(message);

    }

    protected void broadcastData(string message) {

        foreach (Connection connection in peers.Values) {
            connection.sendData(message);
        }

    }
    */
    protected string receiveCommand(byte id) {
        string message = peers[id].receiveCommand();
        Debug.Log("received...");
        Debug.Log(message);
        return message;
    }

    protected void receiveCommand() {

        foreach (Connection connection in peers.Values) {
            Debug.Log("received...");
            string message = connection.receiveCommand();
            Debug.Log(message);
        }

    }

    protected void receiveData() {



    }

    // based on https://forum.unity.com/threads/calculating-latency-c.406536/
    protected int latency(byte id) {
        //currentTime - sentTime = latency
        //currentTime - serverTime + (latency / 2) = synchronizationDelta
        //now + synchronizationDelta = synchedTime

        return 0;
    }

}
