using System.Collections;
using System.Collections.Generic;

using SimpleJSON;

public struct Latency_M {

    static string ToString(byte id) {

        JSONNode root = JSON.Parse("{}");
        root["id"] = id;

        return root.ToString();

    }

}