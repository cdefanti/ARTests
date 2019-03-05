using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class NetworkStatistics {

    ConcurrentDictionary<string, long> sent;
    ConcurrentDictionary<string, long> received;

    // initialize with all the message types this session should expect
    NetworkStatistics(List<string> messageTypes)
    {
        foreach(var type in messageTypes)
        {
            sent[type] = 0;
            received[type] = 0;
        }
    }

    void tickSent(string messageType)
    {
        sent[messageType]++;
    }

    void tickReceived(string messageType)
    {
        received[messageType]++;
    }

    void toString()
    {
        foreach(var key in sent.Keys)
        {
            Debug.Log("S: " + key + " " + this.sent[key].ToString());
            Debug.Log("R: " + key + " " + this.sent[key].ToString());
        }
    }

}
