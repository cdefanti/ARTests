using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class NetworkStatistics {

    ConcurrentDictionary<string, ulong> sent;
    ConcurrentDictionary<string, ulong> received;

    // ideas on things to add dropped packets (??? probably pointless), exceptions, moving averages for some notion of throughput
    ulong sentBytes;
    ulong receivedBytes;

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

    public void tickSent(string messageType, ulong size)
    {
        sent[messageType]++;
        sentBytes += size;
    }

    public void tickReceived(string messageType, ulong size)
    {
        received[messageType]++;
        receivedBytes += size;
    }

    public override string ToString()
    {
        string ret = "";

        foreach(var key in sent.Keys)
        {
            ret += "S: " + key + " " + this.sent[key].ToString() + "\n";
            ret += "R: " + key + " " + this.sent[key].ToString() + "\n";
        }

        return ret;
    }
}
