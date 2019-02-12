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
    public byte id;

    // TODO: probably a better way to sync these up
    public byte[] KnownIDs;
    public string[] KnownHosts;
    public ushort[] KnownPorts;
    // maybe this should be a map to a custom struct instead of a tuple, makes it more readable later
    public Dictionary<byte, NetworkConfig> NetworkConfiguration;

    UDPSandboxPeer[] peers;

    // Use this for initialization
    public void Start() {
        // this is not needed, but useful for finding address in an ad-hoc network
        string hostName = Dns.GetHostName();
        foreach (IPAddress addr in Dns.GetHostEntry(hostName).AddressList) {
            Debug.Log("UNITY: IP Address: " + addr.ToString());
            // set id to 0 to auto-pick id based on IP
            if (id == 0)
            {
                for (int i = 0; i < KnownHosts.Length; i++)
                {
                    if (KnownHosts[i] == addr.ToString())
                    {
                        id = KnownIDs[i];
                    }
                }
            }
        }

        peers = FindObjectsOfType<UDPSandboxPeer>();
        NetworkConfiguration = new Dictionary<byte, NetworkConfig>();
        // TODO: check for KnownX length mismatch.
        for (int i = 0; i < KnownIDs.Length; i++)
        {
            NetworkConfiguration[KnownIDs[i]] = new NetworkConfig(KnownHosts[i], KnownPorts[i]);
        }
    }

    public string GetHostname()
    {
        return NetworkConfiguration[id].hostname;
    }

    public ushort GetPort()
    {
        return NetworkConfiguration[id].port;
    }
	
	// Update is called once per frame
	void Update () {
        Broadcast(Pose_M.ToString(id));
    }

    public UDPSandboxPeer GetPeer(byte id)
    {
        return peers[id];
    }

    public void Broadcast(string message)
    {
        Debug.Log("Peer " + id + " broadcasting: " + message);
        foreach (UDPSandboxPeer peer in peers)
        {
            peer.SendMessage(message);
        }
    }

    public void BroadcastData(JSONNode data, string type)
    {
        JSONNode root = JSON.Parse("{}");
        root["id"] = id;
        root["type"] = type;
        root["info"] = data;
        // TODO: last packet id
        //lastPacketID++;
        //root["packetID"] = lastPacketID;
        string message = root.ToString();
        Broadcast(message);
    }
}
