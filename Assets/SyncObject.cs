using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class SyncObject : MonoBehaviour {

    //public NetworkManager network;
    public UDPSandboxPeer peer;
    public byte ownerId;
    public byte objId;

	// Use this for initialization
	void Start () {
        //network.Clients[ownerId].objects[objId] = new MonoBehaviour();
	}
	
	// Update is called once per frame
	void Update () {
		if (peer.id == ownerId)
        {
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;
            JSONNode info = JSON.Parse("{}");
            info["id"] = objId;
            info["rot"] = JSON.Parse("{}");
            info["rot"]["x"] = rot.x;
            info["rot"]["y"] = rot.y;
            info["rot"]["z"] = rot.z;
            info["rot"]["w"] = rot.w;
            info["pos"] = JSON.Parse("{}");
            info["pos"]["x"] = pos.x;
            info["pos"]["y"] = pos.y;
            info["pos"]["z"] = pos.z;
            peer.BroadcastData(info, "POSE_OBJECT");
        } else
        {
            if (peer.peerClients.ContainsKey(ownerId))
            {
                Pose p = peer.peerClients[ownerId].objects[objId];
                transform.rotation = Quaternion.identity;
                transform.Rotate(Vector3.up, peer.peerClients[ownerId].rot_diff);
                transform.position = transform.rotation * (peer.peerClients[ownerId].pos_diff + p.pos);
                transform.rotation = transform.rotation * p.rot;
            }
        }
	}
}
