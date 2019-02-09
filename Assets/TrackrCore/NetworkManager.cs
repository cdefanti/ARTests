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
    public byte id; // TODO: read from file saved locally to device

    // TODO: probably a better way to sync these up
    public byte[] KnownIDs;
    public string[] KnownHosts;
    public ushort[] KnownPorts;
    // maybe this should be a map to a custom struct instead of a tuple, makes it more readable later
    public Dictionary<byte, NetworkConfig> NetworkConfiguration;

    // Use this for initialization
    public void Start() {
        // this is not needed, but useful for finding address in an ad-hoc network
        string hostName = System.Net.Dns.GetHostName();
        foreach (IPAddress addr in System.Net.Dns.GetHostEntry(hostName).AddressList) {
            Debug.Log("UNITY: IP Address: " + addr.ToString());
        }

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
    }
}
