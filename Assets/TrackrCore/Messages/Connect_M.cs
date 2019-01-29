using System.Collections;
using System.Collections.Generic;

using SimpleJSON;

public struct Connect_M {
    public static string ToString(byte id) {
        // send connection packet so the other client can perform a handshake
        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "CONNECT";
        root["info"] = "";

        return root.ToString();
    }
}

public struct Connect_ACK_M
{
    public static string ToString(byte id) {
        // send connection packet so the other client can perform a handshake
        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "CONNECT_ACK";
        root["info"] = "";

        return root.ToString();
    }
}