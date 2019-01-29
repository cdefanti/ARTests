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
    public Queue<string> messages = new Queue<string>(100);

    public UdpClient client;

    public Dictionary<int, Pose> objects;

    public void pushMessage(string m) {
        messages.Enqueue(m);
    }

};