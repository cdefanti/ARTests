using System.Collections;
using System.Collections.Generic;

using SimpleJSON;

public struct Disconnect_M
{
    public static string ToString(byte id)
    {
        // send connection packet so the other client can perform a handshake
        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "DISCONNECT";
        root["info"] = "";

        return root.ToString();
    }
}

public struct Disconnect_ACK_M
{
    public static string ToString(byte id)
    {
        // send connection packet so the other client can perform a handshake
        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "DISCONNECT_ACK";
        root["info"] = "";

        return root.ToString();
    }
}