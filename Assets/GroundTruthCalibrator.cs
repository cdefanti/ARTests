using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundTruthCalibrator : MonoBehaviour {

    public Tracker groundTruthTracker;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (groundTruthTracker.tracked)
        {
            this.transform.position = groundTruthTracker.transform.rotation * (groundTruthTracker.transform.position - groundTruthTracker.realPos);
            this.transform.rotation = groundTruthTracker.transform.rotation;
        }
	}
}
