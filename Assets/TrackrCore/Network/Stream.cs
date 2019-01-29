using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System;

using UnityEngine;

public abstract class Stream {

    protected const int MaxBufferSize = 1024;
    protected NetworkStream stream;
    protected Byte[] buffer;
    protected IPEndPoint endpoint;
    protected IPEndPoint remoteEndpoint;

    // The serializeEndpoint method serializes the endpoint and returns the 
    // SocketAddress containing the serialized endpoint data.
    protected SocketAddress serializeEndpoint() {

        // Serialize IPEndPoint details to a SocketAddress instance.
        SocketAddress socketAddress = endpoint.Serialize();

        // Display the serialized endpoint information.
        //Debug.Log("Endpoint.Serialize() : " + socketAddress.ToString());

        //Debug.Log("Socket.Family : " + socketAddress.Family);
        //Debug.Log("Socket.Size : " + socketAddress.Size);

        return socketAddress;
    }

    protected void displayEndpointInfo() {

        Debug.Log("Endpoint.Address : " + endpoint.Address);
        Debug.Log("Endpoint.AddressFamily : " + endpoint.AddressFamily);
        Debug.Log("Endpoint.Port : " + endpoint.Port);
        Debug.Log("Endpoint.ToString() : " + endpoint.ToString());

    }
}