using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using OpenCvSharp;
using SimpleJSON;
using System.Net.Sockets;

public class Tracker : MonoBehaviour
{

    
    public byte id;
    public Vector3 center;
    public Vector2 size;
    
    public GameObject TrackerMesh;
    public Vector3 realPos;
    public Quaternion realRot = Quaternion.identity;
    private CameraCalibrationManager calib;
    private ARMarkerDetector detector;
    private Camera cam;


    public bool tracked;

    //public UDPSandboxPeer peer;

    // physical screen size in meters
    private float screenW = 0.15f;
    private float screenH = 0.075f;

    private Quaternion orientationOffset = Quaternion.Euler(180f, 0f, 0f);

    // Use this for initialization
    public void Start()
    {
        calib = FindObjectOfType<CameraCalibrationManager>();
        detector = FindObjectOfType<ARMarkerDetector>();
        cam = FindObjectOfType<Camera>();
    }

    // Update is called once per frame
    public void Update()
    {
        TrackerMesh.SetActive(tracked);

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
                    // TODO: establish client to connect, store in peerClient
                    //TcpClient c = new TcpClient();
                    //peer.Connect(id, c);
                }
            }
        }


        // standard OpenCV algorithm to find trackers
        // finds tracker pose in frame of camera
        Point2f[] corners;
        tracked = false;
        if (!detector.GetTrackerCorners(id, out corners))
        {
            return;
        }
        Vector3[] real_corners = new Vector3[4];
        real_corners[0] = center + new Vector3(-size.x, size.y, 0f) / 2f;
        real_corners[1] = center + new Vector3(size.x, size.y, 0f) / 2f;
        real_corners[2] = center + new Vector3(size.x, -size.y, 0f) / 2f;
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
        // position and rotation of tracker relative to our position
        Vector3 relPos = new Vector3((float)tvec[0], (float)tvec[1], (float)tvec[2]);
        float rnorm = Vector3.Magnitude(new Vector3((float)rvec[0], (float)rvec[1], (float)rvec[2]));
        Quaternion relRot = Quaternion.AngleAxis(rnorm * 180f / Mathf.PI, 
            new Vector3((float)rvec[0] / rnorm, (float)rvec[1] / rnorm, (float)rvec[2] / rnorm));
        Vector3 pos = relPos;
        relPos = cam.transform.rotation * pos;
        pos = relPos + cam.transform.position;

        transform.position = pos;
        //transform.rotation = cam.transform.rotation * relRot;
        
        tracked = true;
    }
}