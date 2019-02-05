using System.Collections;
using System.Collections.Generic;

public struct NetworkConfig 
{
    public string hostname;
    public ushort port;

    public NetworkConfig(string hostname, ushort port)
    {
        this.hostname = hostname;
        this.port = port;
    }
}
