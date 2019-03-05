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

    Dictionary<byte, UDPSandboxPeer> peers;

    // Use this for initialization
    public void Start() {

        PeerDiscovery.PeerJoined = ip => Console.WriteLine("JOINED:" + ip);
        PeerDiscovery.PeerLeft = ip => Console.WriteLine("LEFT:" + ip);

        PeerDiscovery.Start();

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

        peers = new Dictionary<byte, UDPSandboxPeer>();
        UDPSandboxPeer[] peerlist = FindObjectsOfType<UDPSandboxPeer>();
        foreach (UDPSandboxPeer peer in peerlist)
        {
            peers[peer.id] = peer;
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
        Broadcast(Pose_M.ToString(id));
    }

    public UDPSandboxPeer GetPeer(byte id)
    {
        return peers[id];
    }

    public void Broadcast(string message)
    {
        foreach (UDPSandboxPeer peer in peers.Values)
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

/* TODO: Move to global manager
   byte FindClosestPeer()
   {

       byte minPeer = 0xFF;
       long minDistance = long.MaxValue;

       foreach (byte peerID in peerClients.Keys)
       {
           if (peerClients[peerID].latency < minDistance)
           {
               minPeer = peerID;
               minDistance = peerClients[peerID].latency;
           }
       }

       return minPeer;

   }

   void PingAll() {

       foreach (byte peerID in peerClients.Keys) {
           try {

               Ping(peerID);

           } catch (Exception ex) {
               if (ex is SocketException) {
                   Debug.Log("Socket exception " + id + "@" + peerID + ": " + ex);
                   return;
               } else if (ex is InvalidOperationException) {
                   Debug.Log("InvalidOperationException " + id + "@" + peerID + ": " + ex);
                   return;
               }

               throw;
           }
       }
   }
   */

/*
void timesync()
{
    int latency = currentTime - sentTime;
    int synchonizationDelta = currentTime - serverTime + (latency / 2);
    int syncedTime = now + synchonizationDelta;
}
*/

/* TODO: Move to global manager
void OnSpanningTree()
{

}


void MST()
{
    foreach (byte peerID in peerClients.Keys)
    {

    }
}
*/

//            else if (root["type"] == "SPANNINGTREE")
//            {
//                //n[i], parent = recv(nbr i)
//                var id = root["id"];
//                var p = root["info"]["parent"];

//                var d = peerClients[FindClosestPeer()].latency;
//                //var d = min(n) + 1;
//                //d = min(n) + 1;

//                /*
//                parent = find(i s.t.n[i] = d - 1);

//                send(parent, < d = d, parent = true >);

//                for n in nbr {
//                    if n != parent {
//                        send(n, < d = d, parent = false >)
//                    }
//                }
//                */
//            }