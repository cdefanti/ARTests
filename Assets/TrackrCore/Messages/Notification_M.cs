using System.Collections;
using System.Collections.Generic;

using SimpleJSON;

public struct Notification_M
{

    public static string ToString(byte id, string message)
    {

        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "NOTIFICATION";
        root["message"] = message;
        root["info"] = "";

        return root.ToString();
    }

}