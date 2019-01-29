using UnityEngine;

public class Pose {
    
    // relpos is the vector from the Client's tracker to our tracker, as observed by the client
    public Vector3 relPos {get; set;}
    // pos and rot are the local pose of the Client
    public Vector3 pos {get; set;}
    public Quaternion rot {get; set;}

    // differences in local frames between this Client and us
    // rotation is an angle, not a quaternion because rotation only drifts in up-axis
    public float rotDelta = 0f;
    public Vector3 posDelta = new Vector3();
    
}