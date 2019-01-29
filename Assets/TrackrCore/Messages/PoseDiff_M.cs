using System.Collections;
using System.Collections.Generic;

using SimpleJSON;
using UnityEngine;

public struct PoseDiff_M
{

    static string ToString(byte id, Vector3 dpos, Quaternion drot)
    {

        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = "POSE_SELF";

        root["info"] = JSON.Parse("{}");

        root["info"]["drot"] = JSON.Parse("{}");
        root["info"]["drot"]["x"] = drot.x;
        root["info"]["drot"]["y"] = drot.y;
        root["info"]["drot"]["z"] = drot.z;
        root["info"]["drot"]["w"] = drot.w;

        root["info"]["dpos"] = JSON.Parse("{}");
        root["info"]["dpos"]["x"] = dpos.x;
        root["info"]["dpos"]["y"] = dpos.y;
        root["info"]["dpos"]["z"] = dpos.z;

        // might be useless to pass the LPF alpha
        root["info"]["alpha"] = 0.1f;

        return root.ToString();

    }

}
