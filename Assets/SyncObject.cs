using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class SyncObject : MonoBehaviour {

    public NetworkManager network;
    public int ownerId;
    public int objId;

	// Use this for initialization
	void Start () {
        //network.Clients[ownerId].objects[objId] = new MonoBehaviour();
	}
	
	// Update is called once per frame
	void Update () {
		if (network.myId == ownerId)
        {
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;
            JSONNode root = JSON.Parse("{}");
            root["id"] = ownerId;
            root["type"] = "POSE_OBJECT";
            root["info"] = JSON.Parse("{}");
            root["info"]["id"] = objId;
            root["info"]["rot"] = JSON.Parse("{}");
            root["info"]["rot"]["x"] = rot.x;
            root["info"]["rot"]["y"] = rot.y;
            root["info"]["rot"]["z"] = rot.z;
            root["info"]["rot"]["w"] = rot.w;
            root["info"]["pos"] = JSON.Parse("{}");
            root["info"]["pos"]["x"] = pos.x;
            root["info"]["pos"]["y"] = pos.y;
            root["info"]["pos"]["z"] = pos.z;
            network.StageData(root.ToString());
        } else
        {
            Pose p = network.GetObjectPoseData(ownerId, objId);
            transform.rotation = Quaternion.identity;
            transform.Rotate(Vector3.up, network.Clients[ownerId].rot_diff);
            transform.position = transform.rotation * (network.Clients[ownerId].pos_diff + p.position);
            transform.rotation = transform.rotation * p.rotation;
        }
	}
}
