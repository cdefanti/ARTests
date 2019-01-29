using System.Collections;
using System.Collections.Generic;

using SimpleJSON;

public struct Nonvisible_M {

    public static string ToString(byte id) {

        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "NONVISIBLE";
        root["info"] = "";

        return root.ToString();
    }

}

public struct Nonvisible_ACK_M {

    public static string ToString(byte id, bool success) {

        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "NONVISIBLE_ACK";
        root["info"] = "";
        root["success"] = success;

        return root.ToString();
    }
}