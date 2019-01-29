using System.Collections;
using System.Collections.Generic;

using SimpleJSON;
/*
 * 
 * https://cs.nyu.edu/~apanda/assets/papers/nsdi13.pdf
• to reverse: List containing a subset of the node’s
links, initialized to the node’s incoming links in the
given graph G.
Each node also keeps for each link L:
• direction[L]: In or Out; initialized to the direction
according to the given graph G.Per name, this
variable indicates this node’s view of the direction
of the link L.
• local seq[L]: One-bit unsigned integer; initialized
to 0. This variable is akin to a version or sequence
number associated with this node’s view of
link L’s direction.
• remote seq[L]: One-bit unsigned integer; initialized
to 0. This variable attempts to keep track of the
version or sequence number at the neighbor at the
other end of link L.
*/

public struct DDC_M {

    string ToString(byte id) {
        // send connection packet so the other client can perform a handshake
        JSONNode root = JSON.Parse("{}");

        root["id"] = id;
        root["type"] = "CONNECT";
        root["info"] = "";

        return root.ToString();
    }
}