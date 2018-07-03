using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoseLogger : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Debug.Log(string.Format("POSE {0}: ({1}, {2}, {3}), ({4}, {5}, {6}, {7})", name, transform.position.x, transform.position.y, transform.position.z,
            transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w));
	}
}
