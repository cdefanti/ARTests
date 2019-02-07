using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Tracker))]
public class UDPSandboxPeerGUI : Editor {

    int heartbeat = 2000;
    int keepalive = 10000;
    //string peerID = "0";
    int selectedGridIndex = 0;

    string[] peerIDs;
    string notificationField = "notify";

    public class StringAsIntComparer : IComparer
    {
        public int Compare(object l, object r)
        {
            int left = Int32.Parse((string)l);
            int right = Int32.Parse((string)r);
            return left.CompareTo(right);
        }
    }

    public override void OnInspectorGUI()
    {
        Tracker node = (Tracker)target;

        Tracker[]objs = GameObject.FindObjectsOfType(typeof(Tracker)) as Tracker[];

        peerIDs = new string[objs.Length];

        
        int i = 0;
        foreach (Tracker peer in objs)
        {
            //if (node.id == peer.id)
            //{
            //    continue;
            //}
            peerIDs[i++] = peer.id.ToString();
        }

        Array.Sort(peerIDs, new StringAsIntComparer());

        //DrawDefaultInspector();
        GUI.skin.label.alignment = TextAnchor.MiddleLeft;
        GUI.skin.button.alignment = TextAnchor.MiddleLeft;

        GUILayout.Label("Peer ID");

        GUILayout.BeginHorizontal();
        selectedGridIndex = GUILayout.SelectionGrid(selectedGridIndex, peerIDs, 3);
        GUILayout.EndHorizontal();

        

        if (GUILayout.Button("Connect"))
        {
            // destination
            byte peerID = (byte)Convert.ToUInt16(peerIDs[selectedGridIndex]);

            node.messageQueue.Enqueue(new KeyValuePair<byte, string>(peerID, Connect_M.ToString(node.id)));

            //node.SendMessage(peerID, Connect_M.ToString(node.id));
            
        }

        if (GUILayout.Button("Disconnect"))
        {
            // destination
            byte peerID = (byte)Convert.ToUInt16(peerIDs[selectedGridIndex]);
            node.SendMessage(peerID, Disconnect_M.ToString(node.id));
            //node.Disconnect(peerID);
        }

        if (GUILayout.Button("Visible"))
        {
            
            // destination
            byte peerID = (byte)Convert.ToUInt16(peerIDs[selectedGridIndex]);
            node.SendMessage(peerID, Visible_M.ToString(node.id));

        }

        if (GUILayout.Button("No Longer Visible"))
        {
            // destination
            byte peerID = (byte)Convert.ToUInt16(peerIDs[selectedGridIndex]);
            node.SendMessage(peerID, Nonvisible_M.ToString(node.id));
        }

        if (GUILayout.Button("Test Latency"))
        {

        }
        
        if (GUILayout.Button("Sync Clocks"))
        {

        }

        GUILayout.Label("Keep-Alive Heartbeat Period");

        keepalive = (int)GUILayout.HorizontalSlider(keepalive, 100.0f, 100000.0f);
        GUILayout.Label(Convert.ToString(keepalive) + "ms");

        if (GUILayout.Button("Update Timeout"))
        {

        }

        GUILayout.Label("Pose Heartbeat Period");

        heartbeat = (int)GUILayout.HorizontalSlider(heartbeat, 100.0f, 100000.0f);
        GUILayout.Label(Convert.ToString(heartbeat) + "ms");

        if (GUILayout.Button("Update Heartbeat"))
        {
            //node.heartbeat.period = heartbeat;
            //node.heartbeat.resetTimer();
            node.heartbeat = new Heartbeat(heartbeat, 1.0f);
        }

        if (GUILayout.Button("Stop Heartbeat"))
        {
            node.heartbeat.stop();
        }

        notificationField = GUILayout.TextArea(notificationField);

        if (GUILayout.Button("Send Notification"))
        {
            byte peerID = (byte)Convert.ToUInt16(peerIDs[selectedGridIndex]);
            node.SendMessage(peerID, Notification_M.ToString(node.id, notificationField));
        }

        if (GUILayout.Button("Broadcast Notification"))
        {
            node.Broadcast(Notification_M.ToString(node.id, notificationField));
        }

        if (Application.isPlaying)
        {
            foreach (byte u in node.peerClients.Keys)
            {
                GUILayout.Label(Convert.ToString(u));
            }
        }
        

        base.OnInspectorGUI();
    }
}
