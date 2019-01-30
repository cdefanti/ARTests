using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;

public class VRClient {

    public int TrackerID;
    public string IP;
    public bool visible;
    public bool connected;

    public UdpClient client;

    public Dictionary<byte, Pose> objects;

    public int lastPacketID = 0;

    // relpos is the vector from the Client's tracker to our tracker, as observed by the client
    public Vector3 relPos;
    // pos and rot are the local pose of the Client
    public Vector3 pos;
    public Quaternion rot;

    // differences in local frames between this Client and us
    // rotation is an angle, not a quaternion because rotation only drifts in up-axis
    public float rot_diff = 0f;
    public Vector3 pos_diff = new Vector3();

    public void SetRot(Quaternion q)
    {
        rot = q;
    }

    public void SetPos(Vector3 p)
    {
        pos = p;
    }

    public void SetRelPos(Vector3 p)
    {
        relPos = p;
    }
};