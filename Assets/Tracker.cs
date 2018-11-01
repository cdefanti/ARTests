using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using OpenCvSharp;
using SimpleJSON;

public class Tracker : MonoBehaviour {

    public ARMarkerDetector detector;
    public int id;
    public Vector3 center;
    public Vector2 size;
    public Camera cam;
    public Vector3 realPos;
    public CameraCalibrationManager calib;
    public GameObject TrackerMesh;

    public bool tracked;

    public NetworkManager network;

    // physical screen size in meters
    private float screenW = 0.15f;
    private float screenH = 0.075f;

    private Quaternion orientationOffset = Quaternion.Euler(180f, 0f, 0f);

    private float rot_diff = 0f;
    private Vector3 pos_diff = Vector3.zero;
    private float diff_alpha = 1f;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
        TrackerMesh.SetActive(tracked);
        if (network.Clients[id].connected)
        {
            TrackerMesh.GetComponent<Renderer>().material.color = Color.green;
        } else
        {
            TrackerMesh.GetComponent<Renderer>().material.color = Color.white;
        }
        
        if (tracked)
        {
            // tap on a tracker to connect to that client
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                Vector2 touchpos = Input.GetTouch(0).position;
                Vector3 worldTouchPoint = cam.ScreenToWorldPoint(new Vector3(touchpos.x, touchpos.y, cam.nearClipPlane));
                // check quadratic to see if it hits
                float r = Mathf.Max(size.x, size.y);
                Vector3 campos = cam.transform.position;
                Vector3 w = Vector3.Normalize(worldTouchPoint - campos);
                Vector3 p = campos - transform.position;
                float wp = Vector3.Dot(w, p);
                float d = wp * wp - Vector3.Dot(p, p) + r * r;
                if (d >= 0)
                {
                    // hit
                    bool connectOk = network.Connect(id);
                }
            }
        }

        // standard OpenCV algorithm to find trackers
        // finds tracker pose in frame of camera
        Point2f[] corners;
        tracked = false;
        if (!detector.GetTrackerCorners(id, out corners))
        {
            diff_alpha = 1f;
            return;
        }
        Vector3[] real_corners = new Vector3[4];
        real_corners[0] = center + new Vector3(-size.x,  size.y, 0f) / 2f;
        real_corners[1] = center + new Vector3( size.x,  size.y, 0f) / 2f;
        real_corners[2] = center + new Vector3( size.x, -size.y, 0f) / 2f;
        real_corners[3] = center + new Vector3(-size.x, -size.y, 0f) / 2f;

        Point3f[] real_corners_cv = new Point3f[4];
        for (int i = 0; i < 4; i++)
        {
            real_corners_cv[i] = new Point3f(real_corners[i].x, real_corners[i].y, real_corners[i].z);
        }
        float focalLength = 0f;
        List<CameraMetadataValue> data = new List<CameraMetadataValue>();
        Frame.CameraMetadata.TryGetValues(CameraMetadataTag.LensFocalLength, data);
        if (data.Count > 0)
            focalLength = data[0].AsFloat();

        double[,] cam_mat = calib.getCameraMat();

        double[] distortion = calib.getDistCoeffs();
        double[] rvec, tvec;
        
        Cv2.SolvePnP(real_corners_cv, corners, cam_mat, distortion, out rvec, out tvec);


        // transform tracker pos from camera coordinates to global coordinates
        Quaternion rot = Quaternion.identity;
        Vector3 rawpos = new Vector3((float)tvec[0], (float)tvec[1], (float)tvec[2]);
        Vector3 pos = rawpos;
        rawpos = cam.transform.rotation * pos;
        pos = rawpos + cam.transform.position;

        transform.position = pos;

        // send over tracker position in camera frame
        // this is the vector from our client to their client
        JSONNode info = JSON.Parse("{}");
        info["diff"] = JSON.Parse("{}");
        info["diff"]["x"] = rawpos.x;
        info["diff"]["y"] = rawpos.y;
        info["diff"]["z"] = rawpos.z;
        network.SendData(info, "POSE_OTHER", id);

        // if we receive data from them, we can now figure out the positional and rotational difference
        if (network.Clients[id].connected)
        {
            // p12 is the vector from client 1 (us) to client 2 (them) in our local frame
            // p21 is the vector from client 2 to client 1 in their local frame
            // p1, p2, and q2 is the local pose data of each client in their own frame
            // Note: most of these project onto the xz plane because y/up-angle is the only angle that drifts
            Vector3 p12 = rawpos;
            Vector3 p21 = network.GetClientRelPos(id);
            Quaternion q2 = network.GetClientPoseData(id).rotation;
            Vector3 p1 = Frame.Pose.position;
            Vector3 p2 = network.GetClientPoseData(id).position;
            p12 = Vector3.Normalize(Vector3.ProjectOnPlane(p12, Vector3.up));
            p21 = Vector3.Normalize(Vector3.ProjectOnPlane(p21, Vector3.up));
            // forward vectors of 
            Vector3 f1 = Vector3.ProjectOnPlane(Frame.Pose.forward, Vector3.up);
            Vector3 f2 = Vector3.ProjectOnPlane(q2 * Vector3.forward, Vector3.up);
            // a1, a2 are the angles between the forward vector of a client and the vector from the client to the other client
            // we use these angles to find the final correction angle
            float a1 = Vector3.SignedAngle(p12, f1, Vector3.up);
            float a2 = Vector3.SignedAngle(p21, f2, Vector3.up);

            float a = 180f - (a1 - a2);
            // adjust a to account for the rotations already made locally within the system
            a += Vector3.SignedAngle(Vector3.forward, f1, Vector3.up) - Vector3.SignedAngle(Vector3.forward, f2, Vector3.up);

            // finally, use a LPF/complemetary filter to smooth angle/position deltas
            // IDEALLY deltas in rot_diff should be small/0, but in practice this isn't true due to innate CV errors
            // finding a good alpha for the complementary filter is critical in making this all work!
            rot_diff = Mathf.LerpAngle(rot_diff, a, diff_alpha);
            Vector3 p = transform.position - (Quaternion.Euler(0f, rot_diff, 0f) * p2);
            pos_diff = Vector3.Lerp(pos_diff, p, diff_alpha);
            diff_alpha = Mathf.Max(diff_alpha - 0.05f, 0.1f);
            
            // apply final result to virtual tracker
            transform.rotation = Quaternion.identity;
            transform.Rotate(Vector3.up, rot_diff);
            transform.rotation = transform.rotation * q2;


            // update the network manager so that other objects in scene can reference it
            network.Clients[id].rot_diff = rot_diff;
            network.Clients[id].pos_diff = pos_diff;

            //Debug.Log(string.Format("UNITY: a1: {0}, a2: {1}, a: {2}", a1, a2, a));
        }

        tracked = true;
    }
}
