using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class SyncObject : MonoBehaviour {

    public NetworkManager network;
    public byte ownerId;
    public byte objId;
    protected bool shouldSend;

	// Use this for initialization
	void Start () {
        //network.Clients[ownerId].objects[objId] = new MonoBehaviour();
	}
	
	// Update is called once per frame
	void Update () {
        if (shouldSend)
        {
            if (network.id == ownerId)
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
                network.BroadcastData(info, "POSE_OBJECT");
            }
            else
            {
                UDPSandboxPeer peer = network.GetPeer(ownerId);
                if (peer.peerClient.connected)
                {
                    Pose p = peer.peerClient.objects[objId];
                    transform.rotation = Quaternion.identity;
                    transform.Rotate(Vector3.up, peer.peerClient.rot_diff);
                    transform.position = transform.rotation * (peer.peerClient.pos_diff + p.pos);
                    transform.rotation = transform.rotation * p.rot;
                }
            }
        }
	}
}
