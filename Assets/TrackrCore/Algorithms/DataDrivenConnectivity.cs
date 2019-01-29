using System.Collections;
using System.Collections.Generic;

using System;
using System.Threading;

using UnityEngine;

// DDC refers to Data-Driven Connectivity

struct VirtualLink {
    byte vn;
    byte u;
}

public class DDCNode : Node {

    byte id;

    int last_heartbeat;
    int current_heartbeat;

    List<byte> neighbors;
    
    List<VirtualNode> virtualNodes;

    Dictionary<int, byte> receivedHeartbeats;

    Dictionary<byte, bool> neighborLocks;

    //https://stackoverflow.com/questions/2696052/thread-signaling-basics

    //https://docs.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads
    CancellationTokenSource killWatch;
    CancellationTokenSource killConnect;

    List<byte> to_reverse;
    Dictionary<byte, bool> direction;
    Dictionary<byte, bool> local_seq;
    Dictionary<byte, bool> remote_seq;

    Heartbeat heartbeat;

    public DDCNode(byte id, string hostname, ushort port) : base(id, hostname, port) {

        heartbeat = new Heartbeat(2000, 1.0f);

        neighbors = new List<byte>();
        receivedHeartbeats = new Dictionary<int, byte>();
        neighborLocks = new Dictionary<byte, bool>();

        last_heartbeat = -1;
        current_heartbeat = 0;

    }

    void acquireLock(byte u) {

        sendCommand(u, DDC_Lock_M.ToString(id));
        Debug.Log(receiveCommand(u));
        
    }

    void releaseLock(byte u) {

        sendCommand(u, DDC_Release_M.ToString(id));
        Debug.Log(receiveCommand(u));
        
    }

    void allEdgesOutward() {

        //Get locks from { neighbors, self} in increasing ID order
        foreach (byte u in neighbors) {
            acquireLock(u);
        }

        //Create virtual node vn
        VirtualNode vn = new VirtualNode();

        // Create the token source.
        killWatch = new CancellationTokenSource();
        killConnect = new CancellationTokenSource();
        
        //run(thread_watch_for_packets)
        //run(thread_connect_virtual_node)

        ThreadPool.QueueUserWorkItem(new WaitCallback(state => watchForPackets(vn)), killWatch.Token);
        ThreadPool.QueueUserWorkItem(new WaitCallback(state => connectVirtualNode(vn)), killConnect.Token);


        //When all threads complete:
        //release all locks
        foreach (byte u in neighbors) {
            releaseLock(u);
        }
    }

    // make the following two async callbacks, if it executes, kill other thread, delete vn, and exit
    void watchForPackets(VirtualNode vn) {

        //if a data packet arrives at vn
            //kill thread_connect_virtual_node
            killConnect.Cancel();
            //delete vn
            //exit thread_watch_for_packets
        
    }

    void connectVirtualNode(VirtualNode vn) {

        //For each neighbor u of v
        foreach (byte u in neighbors) {
            //Link vn, u with virtual link
            //Signal(LinkDone?) to u
        }

        //After all neighbors ack(LinkDone):
        //For each neighbor u of v
        foreach (byte u in neighbors) {
            //Signal(Dir: vn->u?) to u

        }
        //After all neighbors ack(Dir: vn->u):
        foreach (byte u in neighbors)
        {
            
        }

        //kill thread_watch_for_packets
        killWatch.Cancel();
            //delete old virtual nodes at v
        //exit thread_connect_virtual_node
    }


    bool neighborWithLowerDistance() {

        foreach (byte u in neighbors) {

        }

        return false;
    }

    void triggerHeartbeat() {

        if (last_heartbeat >= current_heartbeat) {
            //Exit
            return;
        }

        //rcvd[H, w] = true
        //If(rcvd[H, u] == true for all neighbors u with lower distance)
            //If(direction[u] = In for any neighbor with lower distance)
                //AEO(v)
            //Send H to all neighbors
            //last_heartbeat_processed = H

    }

// control plane

    bool outgoingLinks() {

        foreach(bool dir in direction.Values) {
            if (dir) {
                return true;
            }
        }

        return false;
    }

    void reverse_in_to_out(byte L) {
        // out is true (1), in is false (0)
        direction[L] = true;
        local_seq[L] = !local_seq[L];
    }
    

    void reverse_out_to_in(byte L) {
        // out is true (1), in is false (0)
        direction[L] = false;
        remote_seq[L] = !remote_seq[L];

    }
    /*
    void update_FIB_on_arrival(packet p, byte L) {
        if (direction[L] == false) {
            //Assert(p.seq == remote_seq[L]);
        } else if (p.seq != remote_seq[L]) {
            reverse_out_to_in(L);
        }
    }
    */
    void update_FIB_on_departure() {

        //if there are no Out links {
        if (!outgoingLinks()) {
            if (to_reverse.Count == 0) {
                // ‘partial’ reversal impossible
                // to_reverse = all links
                to_reverse = neighbors;
            }

            foreach(byte L in to_reverse) {
                reverse_in_to_out(L);
            }

            // Reset reversible links
         //   to_reverse = { L: direction[L] = In }
            
        }
    }
}

class VirtualNode {

    HashSet<VirtualLink> vn;



}