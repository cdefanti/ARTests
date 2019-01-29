using System.Collections;
using System.Collections.Generic;

using SimpleJSON;

public struct DDC_Release_M {

    public static string ToString(byte id) {

        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "DDC_RELEASE";
        root["info"] = "";

        return root.ToString();
    }

}

public struct DDC_Release_ACK_M {

    public static string ToString(byte id, bool success) {

        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "DDC_RELEASE_ACK";
        root["info"] = "";
        root["success"] = success;

        return root.ToString();
    }

}