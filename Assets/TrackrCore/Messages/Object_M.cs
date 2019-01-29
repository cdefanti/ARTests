using System.Collections;
using System.Collections.Generic;

using SimpleJSON;
using UnityEngine;

public struct Object_M
{

    public static string ToString(byte id, byte objectID, Vector3 pos, Quaternion rot)
    {

        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "OBJECT";
        root["obj_id"] = objectID;
        root["info"] = JSON.Parse("{}");

        root["info"]["rot"] = JSON.Parse("{}");
        root["info"]["rot"]["x"] = rot.x;
        root["info"]["rot"]["y"] = rot.y;
        root["info"]["rot"]["z"] = rot.z;
        root["info"]["rot"]["w"] = rot.w;
        root["info"]["pos"] = JSON.Parse("{}");
        root["info"]["pos"]["x"] = pos.x;
        root["info"]["pos"]["y"] = pos.y;
        root["info"]["pos"]["z"] = pos.z;

        //TODO add vertex, normals, color / materials data
        root["info"]["geometry"] = JSON.Parse("{}");

        return root.ToString();
    }

}