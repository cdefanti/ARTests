using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class NetworkStatistics {

    ConcurrentDictionary<string, long> sent;
    ConcurrentDictionary<string, long> received;

    public ulong lastPacketID;

    // initialize with all the message types this session should expect
    NetworkStatistics(List<string> messageTypes)
    {
        lastPacketID = 0;

        foreach(var type in messageTypes)
        {
            sent[type] = 0;
            received[type] = 0;
        }
    }

    public void tickSent(string messageType)
    {
        sent[messageType]++;
    }

    public void tickReceived(string messageType)
    {
        received[messageType]++;
    }

    public void ToString()
    {
        foreach(var key in sent.Keys)
        {
            Debug.Log("S: " + key + " " + this.sent[key].ToString());
            Debug.Log("R: " + key + " " + this.sent[key].ToString());
        }
    }

}
