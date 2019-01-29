using System.Collections;
using System.Collections.Generic;

using SimpleJSON;

public struct DDC_Lock_M {

    public static string ToString(byte id) {

        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "DDC_LOCK";

        return root.ToString();
    }

}

public struct DDC_Lock_ACK_M {

    public static string ToString(byte id, bool success) {
        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "DDC_LOCK_ACK";
        root["success"] = success;

        return root.ToString();
    }

}