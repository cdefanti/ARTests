using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

public struct SpanningTree_M {

    public static string ToString(byte id, bool parent, int distance)
    {
        // send connection packet so the other client can perform a handshake
        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "SPANNINGTREE";
        root["info"] = JSON.Parse("{}");
        root["info"]["distance"] = distance;
        root["info"]["parent"] = parent;

        return root.ToString();
    }

}
