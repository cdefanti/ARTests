// based on:
// https://stackoverflow.com/questions/32103790/how-to-get-ip-addresses-of-hosts-on-local-network-running-my-program

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Caching; // add this library from the reference tab
using System.Text;
using System.Threading.Tasks;

/*
Discoverer.PeerJoined = ip => Console.WriteLine("JOINED:" + ip);
Discoverer.PeerLeft= ip => Console.WriteLine("LEFT:" + ip);

Discoverer.Start();
*/

// simple service discovery protocol
public class Discoverer
{
    static string MULTICAST_IP = "239.255.255.250";
    static int MULTICAST_PORT = 1900;

    static UdpClient _UdpClient;
    static MemoryCache _Peers = new MemoryCache("_PEERS_");

    public static Action<string> PeerJoined = null;
    public static Action<string> PeerLeft = null;

    public static void Start()
    {
        _UdpClient = new UdpClient();
        _UdpClient.Client.Bind(new IPEndPoint(IPAddress.Any, MULTICAST_PORT));
        _UdpClient.JoinMulticastGroup(IPAddress.Parse(MULTICAST_IP));


        Task.Run(() => Receiver());
        Task.Run(() => Sender());
    }

    static void Sender()
    {
        var IamHere = Encoding.UTF8.GetBytes("I AM ALIVE");
        IPEndPoint mcastEndPoint = new IPEndPoint(IPAddress.Parse(MULTICAST_IP), MULTICAST_PORT);

        while (true)
        {
            _UdpClient.Send(IamHere, IamHere.Length, mcastEndPoint);
            Task.Delay(1000).Wait();
        }
    }

    static void Receiver()
    {
        var from = new IPEndPoint(0, 0);
        while (true)
        {
            _UdpClient.Receive(ref from);
            if (_Peers.Add(new CacheItem(from.Address.ToString(), from),
                new CacheItemPolicy()
                {
                    SlidingExpiration = TimeSpan.FromSeconds(20),
                    RemovedCallback = (x) => { if (PeerLeft != null) PeerLeft(x.CacheItem.Key); }
                }
                )
            )
            {
                if (PeerJoined != null) PeerJoined(from.Address.ToString());
            }

            Console.WriteLine(from.Address.ToString());
        }
    }
}