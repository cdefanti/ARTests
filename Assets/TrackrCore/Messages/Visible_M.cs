using System.Collections;
using System.Collections.Generic;

using SimpleJSON;

public struct Visible_M
{

    public static string ToString(byte id)
    {

        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "VISIBLE";
        root["info"] = "";

        return root.ToString();
    }

}

public struct Visible_ACK_M
{

    public static string ToString(byte id, bool success)
    {

        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "VISIBLE_ACK";
        root["info"] = "";
        root["success"] = success;

        return root.ToString();
    }

}