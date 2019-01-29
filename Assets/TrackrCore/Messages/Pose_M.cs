using System.Collections;
using System.Collections.Generic;

using SimpleJSON;
using UnityEngine;

public struct Pose_M {

    static string ToString(byte id) {

        // broadcast our pose to everyone we're connected to
        Vector3 pos = GoogleARCore.Frame.Pose.position;
        Quaternion rot = GoogleARCore.Frame.Pose.rotation;

        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "POSE_SELF";
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

        return root.ToString();

    }

}
