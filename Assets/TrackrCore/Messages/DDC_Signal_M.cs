using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

public struct DDC_Signal_M
{

    string ToString(byte id)
    {
        // send connection packet so the other client can perform a handshake
        JSONNode root = JSON.Parse("{}");

        root["id"] = id;
        root["type"] = "DDC_SIGNAL";
        root["info"] = JSON.Parse("{}");
        root["info"]["done"] = true;

        return root.ToString();
    }
}