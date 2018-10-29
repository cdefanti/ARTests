using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System;
using SimpleJSON;

public class NetworkManager : MonoBehaviour {

    public int myId;

    public int[] KnownIds;
    public string[] KnownIps;

    public Dictionary<int, VRClient> Clients;

    public class VRClient
    {
        public int TrackerID;
        public string IP;
        public bool visible;
        public bool connected;
        public Queue<string> messages = new Queue<string>(100);

        public UdpClient client;

        // relpos is the vector from the Client's tracker to our tracker, as observed by the client
        public Vector3 relPos;
        // pos and rot are the local pose of the Client
        public Vector3 pos;
        public Quaternion rot;

        // differences in local frames between this Client and us
        // rotation is an angle, not a quaternion because rotation only drifts in up-axis
        public float rot_diff = 0f;
        public Vector3 pos_diff = new Vector3();

        public Dictionary<int, Pose> objects;

        public void pushMessage(string m)
        {
            messages.Enqueue(m);
        }

        public void setRot (Quaternion q)
        {
            rot = q;
        }

        public void setPos(Vector3 p)
        {
            pos = p;
        }

        public void setRelPos (Vector3 p)
        {
            relPos = p;
        }
    };

    public int PORT = 20000;

    bool active;

    float updateTime;

    UdpClient client;
    Thread pthread;

    object lockobj;

    IPEndPoint ep;

    // Use this for initialization
    void Start()
    {
        // instantiate clients -- ideally, later on this could be done more flexibly
        Clients = new Dictionary<int, VRClient>();
        if (KnownIds.Length == KnownIps.Length)
        {
            for (int i = 0; i < KnownIds.Length; i++)
            {
                VRClient newclient = new VRClient
                {
                    TrackerID = KnownIds[i],
                    IP = KnownIps[i],
                    visible = false,
                    connected = false,
                    rot = Quaternion.identity,
                    pos = Vector3.zero,
                    relPos = Vector3.zero,
                    objects = new Dictionary<int, Pose>()
                };
                Clients[KnownIds[i]] = newclient;
                
            }
        }

        // these are used to maintain asynchronous data receiving
        active = true;
        lockobj = new object();

        // create socket and initiate other parameters
        ep = new IPEndPoint(IPAddress.Parse(Clients[myId].IP), PORT + myId);
        client = new UdpClient();
        client.ExclusiveAddressUse = false;
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        Debug.Log("opening connection...");
        Debug.Log(ep.Address.ToString() + ", " + ep.Port);
        client.Client.Bind(ep);

        // this is not needed, but useful for finding address in an ad-hoc network
        string hostName = System.Net.Dns.GetHostName();
        foreach (IPAddress addr in System.Net.Dns.GetHostEntry(hostName).AddressList)
        {
            Debug.Log("UNITY: IP Address: " + addr.ToString());
        }
        
        // start async receive
        pthread = new Thread(DataListen);
        pthread.Start();
    }

    public bool Connect (int id)
    {
        // maybe we can improve on this by allowing us to create a client upon connection
        if (!Clients.ContainsKey(id)) {
            Debug.Log("no client associated with key " + id);
            return false;
        }
        VRClient c = Clients[id];
        if (c.connected)
        {
            Debug.Log("UNITY: client " + id + " is already connected");
            return false;
        }

        // bind sender
        IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(Clients[myId].IP), 0);
        c.client = new UdpClient();
        c.client.ExclusiveAddressUse = false;
        c.client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        c.client.Client.Bind(ipep);
        c.connected = true;
        // send connection packet so the other client can perform a handshake
        JSONNode root = JSON.Parse("{}");
        root["id"] = myId;
        root["type"] = "CONNECT";
        root["info"] = "";
        c.pushMessage(root.ToString());
        
        Clients[id] = c;

        // return true to signal connection was OK
        return true;
    }

    // broadcast message to everyone
    public void StageData(string message)
    {
        foreach (int id in KnownIds)
        {
            if (id != myId)
            {
                StageData(message, id);
            }
        }
    }

    // broadcast message to one client
    public void StageData(string message, int id)
    {
        Clients[id].pushMessage(message);
    }

    // the next few methods allow other Unity objects to use stored network data
    // this is for getting the pose of objects received from other clients
    public Pose GetObjectPoseData(int clientId, int objectId)
    {
        if (Clients.ContainsKey(clientId))
        {
            Debug.Log("UNITY: found client");
            if (Clients[clientId].objects.ContainsKey(objectId))
            {
                Debug.Log("UNITY: found object");
                return Clients[clientId].objects[objectId];
            }
        } 
        return new Pose();
    }
    // get the position from another client to us in their local frame
    public Vector3 GetClientRelPos(int id)
    {
        // x, y, z, angle
        return Clients[id].relPos;
    }
    // get the pose of another client in their own local frame
    public Pose GetClientPoseData(int id)
    {
        // x, y, z, angle
        return new Pose(Clients[id].pos, Clients[id].rot);
    }
	
	// Update is called once per frame
	void Update () {
        // broadcast our pose to everyone we're connected to
        Vector3 pos = GoogleARCore.Frame.Pose.position;
        Quaternion rot = GoogleARCore.Frame.Pose.rotation;
        JSONNode root = JSON.Parse("{}");
        root["id"] = myId;
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
        StageData(root.ToString());
        SendData();
    }

    void SendData()
    {
        // batch send all queued messages
        // queue might not be necessary, maybe we want to send things ASAP
        foreach (VRClient c in Clients.Values)
        {
            while (c.connected && c.messages.Count > 0)
            {
                string m = c.messages.Dequeue();
                Byte[] sendbuf = System.Text.Encoding.UTF8.GetBytes(m);
                IPEndPoint sendEP = new IPEndPoint(IPAddress.Parse(c.IP), PORT + c.TrackerID);
                c.client.Send(sendbuf, sendbuf.Length, sendEP);
            }
        }
    }

    private void OnApplicationQuit()
    {
        // this will allow the async thread to terminate
        active = false;
    }

    // simple class for network status, passed in the async thread
    class UdpState : System.Object
    {
        public UdpState(IPEndPoint e, UdpClient c) { this.e = e; this.c = c; }
        public IPEndPoint e;
        public UdpClient c;
    }

    // begins the async receive loop
    void DataListen()
    {
        UdpState state = new UdpState(ep, client);
        client.BeginReceive(new AsyncCallback(ReceiveCallback), state);
        while (active)
        {
            Thread.Sleep(100);
        }
        // active is set to false when the application terminates, then this thread can cleanup
        Debug.Log("closing connection...");
        client.Close();
        foreach (VRClient c in Clients.Values)
        {
            if (c.client != null)
            {
                c.client.Close();
            }
        }
    }

    // all messages are received in JSON right now, this will parse it all out into the data we need
    void ParseJSON(string json)
    {
        var root = JSON.Parse(json);
        int id = root["id"].AsInt;
        if (root["type"] == "CONNECT")
        {
            Connect(id);
        } else if (root["type"] == "POSE_SELF")
        {
            Quaternion rot = new Quaternion(root["info"]["rot"]["x"].AsFloat,
                                         root["info"]["rot"]["y"].AsFloat,
                                         root["info"]["rot"]["z"].AsFloat,
                                         root["info"]["rot"]["w"].AsFloat);
            Vector3 pos = new Vector3(root["info"]["pos"]["x"].AsFloat,
                                      root["info"]["pos"]["y"].AsFloat,
                                      root["info"]["pos"]["z"].AsFloat);
            Clients[id].setRot(rot);
            Clients[id].setPos(pos);
        }  else if (root["type"] == "POSE_OTHER")
        {
             Vector3 relpos = new Vector3(root["info"]["diff"]["x"].AsFloat,
                                          root["info"]["diff"]["y"].AsFloat,
                                          root["info"]["diff"]["z"].AsFloat);
            Clients[id].setRelPos(relpos);
        } else if (root["type"] == "POSE_OBJECT")
        {
            int objId = root["info"]["id"].AsInt;
            Quaternion rot = new Quaternion(root["info"]["rot"]["x"].AsFloat,
                                         root["info"]["rot"]["y"].AsFloat,
                                         root["info"]["rot"]["z"].AsFloat,
                                         root["info"]["rot"]["w"].AsFloat);
            Vector3 pos = new Vector3(root["info"]["pos"]["x"].AsFloat,
                                      root["info"]["pos"]["y"].AsFloat,
                                      root["info"]["pos"]["z"].AsFloat);

            Clients[id].objects[objId] = new Pose(pos, rot);

        }
    }

    // async callback method
    void ReceiveCallback(IAsyncResult ar)
    {
        if (!active)
        {
            return;
        }
        UdpClient c = (UdpClient)((UdpState)(ar.AsyncState)).c;
        IPEndPoint e = (IPEndPoint)((UdpState)(ar.AsyncState)).e;
        // get data
        Byte[] recvbuf = c.EndReceive(ar, ref e);
        // parse data
        string str_data = System.Text.Encoding.UTF8.GetString(recvbuf);
        ParseJSON(str_data);
        // loop the callback
        UdpState state = new UdpState(e, c);
        c.BeginReceive(new AsyncCallback(ReceiveCallback), state);
    }
}
