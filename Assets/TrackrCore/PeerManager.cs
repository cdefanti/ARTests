using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;
//using System.Threading.Channels;

using UnityEngine;

public class PeerManager : MonoBehaviour {

    Dictionary<byte, Peer> peers;

    public byte[] KnownIds;
    public string[] KnownIps;
    public ushort[] Ports;

    // Use this for initialization
    void Start() {

        Debug.Log("creating " + KnownIds.Length + " peers");
        peers = new Dictionary<byte, Peer>();
        for (int i = 0; i < KnownIds.Length; i++) {
            Debug.Log(i);
            peers[(byte)i] = new Peer(KnownIds[i], KnownIps[i], Ports[i]);
        }
        
        //async peers[2].;
        peers[0].connect(2, KnownIps[2]);

    }
	// Update is called once per frame
	void Update () {
		
	}
}
