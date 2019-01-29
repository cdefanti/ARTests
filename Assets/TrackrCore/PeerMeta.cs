using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Net.Sockets;

public class PeerMeta {

    public readonly byte id;
    public readonly ushort port;
    public readonly string hostname;

    bool visible;

    public TcpClient tcpClient;
    public ConcurrentQueue<string> messageQueue;

    public PeerMeta(byte id, string hostname, ushort port)
    {

    }

}
