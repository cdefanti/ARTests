using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;

using System.Text;
using System;

using UnityEngine;

public class DataStream : Stream {

    // simple class for network status, passed in the async thread
    class UdpState : System.Object {
        public UdpState(IPEndPoint e, UdpClient c) { this.e = e; this.c = c; }
        public IPEndPoint e;
        public UdpClient c;
    }

    UdpClient client;

    public DataStream(string hostname, ushort port) {

        // ad-hoc usefulness
        /*
        // Get the object containing Internet host information.
        IPHostEntry host = Dns.GetHostEntry(hostname);

        Socket tempSocket = null;
        SocketAddress serializedSocketAddress = null;

        // Obtain the IP address from the list of IP addresses associated with the server.
        foreach (IPAddress address in host.AddressList) {
            endpoint = new IPEndPoint(address, port);
            Debug.Log(endpoint.Address.ToString() + ", " + endpoint.Port);
        }
        */

        client = new UdpClient();
        client.ExclusiveAddressUse = false;
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        Debug.Log("opening connection...");
        Debug.Log(endpoint.Address.ToString() + ", " + endpoint.Port);

        client.Client.Bind(endpoint);
        // 100 pending connections before connection refused fires
        client.Client.Listen(100);

    }

    public bool write(string message) {

        try {

            int bytes = client.Send(Encoding.ASCII.GetBytes(message), message.Length);
            Debug.Log("sent " + bytes + " to " + endpoint);

            return true;

        } catch (Exception e) {

            Debug.Log(e.ToString());

            return false;

        }
        
    }

    public string read() {

        try {

//            if (client.ReceiveBufferSize > 0) {

  //              stream.Read(buffer, 0, client.ReceiveBufferSize);
                string message = Encoding.ASCII.GetString(buffer); //the message incoming
                Debug.Log("got message " + message.Length + " to " + endpoint);
                return message;
          //  }

        } catch (Exception e) {

            Debug.Log(e.ToString());

        }

        return "";

    }

    // begins the async receive loop
    void DataListen() {
        /*
        UdpState state = new UdpState(endpoint, client);
        client.BeginReceive(new AsyncCallback(ReceiveCallback), state);
        while (active)
        {
            Thread.Sleep(100);
        }

        // active is set to false when the application terminates, then this thread can cleanup
        Debug.Log("closing connection...");
        client.Close();
        foreach (VRClient c in Clients.Values)
        {
            if (c.client != null)
            {
                c.client.Close();
            }
        }
        */
    }

    // async callback method
    void ReceiveCallback(IAsyncResult ar) {
        /*
        if (!active) {
            return;
        }

        UdpClient c = (UdpClient)((UdpState)(ar.AsyncState)).c;
        IPEndPoint e = (IPEndPoint)((UdpState)(ar.AsyncState)).e;
        
        // get data
        Byte[] recvbuf = c.EndReceive(ar, ref e);
        
        // parse data
        string str_data = System.Text.Encoding.UTF8.GetString(recvbuf);
        ParseJSON(str_data);
        // loop the callback
        UdpState state = new UdpState(e, c);
        c.BeginReceive(new AsyncCallback(ReceiveCallback), state);
        */
    }
    

}