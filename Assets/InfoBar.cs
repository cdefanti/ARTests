using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class InfoBar : MonoBehaviour {

    public PoseLogger pose;

    private float time;
    private float update = 0.5f;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        time += Time.deltaTime;
        if (time > update)
        {
            Text text = GetComponent<Text>();
            Vector3 pos = pose.transform.position;
            Vector3 rot = pose.transform.rotation.eulerAngles;
            text.text = string.Format("POS: {0:00.00} {1:00.00} {2:00.00} || ROT {3:000.0} {4:000.0} {5:000.0}", pos.x, pos.y, pos.z, rot.x, rot.y, rot.z);
            time -= update;
        }
	}
}
