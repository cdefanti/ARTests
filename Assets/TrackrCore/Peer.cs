using System.Collections.Generic;

using System.Threading.Tasks;
//using System.Threading.Channels;

using UnityEngine;

public class Peer : DDCNode {

    
    Pose pose;

    //Channel<Connect_M> connectionsChan;
    //Channel<SpanningTree_M> mstChan;
    //Channel<DDC_M, SpanningTree_M> ddcChan;

    public Peer(byte id, string hostname, ushort port) : base(id, hostname, port) {

        // use bounded channels to avoid nuking the memory...
        //connectionsChan = Channel.CreateBounded<Connect_M>(bufferSize);
        //mstChan = Channel.CreateBounded<SpanningTree_M>(bufferSize);
        
    }

    
    /*
    private static async Task ListenToChannel(ChannelReader<string> reader) {

        //if this returns false the channel is completed
        while (await reader.WaitToReadAsync()) {

            //as a note, if there are multiple readers but only one message, only one reader
            //wakes up. This prevents inefficent races. 

            string messageString;
            while (reader.TryRead(out messageString)) {
                Debug.Log($"The listener just read {messageString}!");
                //await Task.Delay(25);
            }
        }
    }
    */
}