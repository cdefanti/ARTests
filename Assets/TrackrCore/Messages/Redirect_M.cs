using System.Collections;
using System.Collections.Generic;

using SimpleJSON;

public struct Redirect_M
{
    public static string ToString(byte id, byte destID)
    {
        // send redirection packet so the next hop can redirect, probably stupid to use this
        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "REDIRECT";
        root["info"] = "";
        root["destination"] = destID;

        return root.ToString();
    }
}