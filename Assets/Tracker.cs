using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using OpenCvSharp;

public class Tracker : MonoBehaviour {

    public ARMarkerDetector detector;
    public int id;
    public Vector3 center;
    public Vector2 size;
    public Camera cam;
    public Vector3 realPos;
    public CameraCalibrationManager calib;

    public bool tracked;

    // physical screen size in meters
    private float screenW = 0.15f;
    private float screenH = 0.075f;

    private Quaternion orientationOffset = Quaternion.Euler(180f, 0f, 0f);

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Point2f[] corners;
        tracked = false;
        if (!detector.GetTrackerCorners(id, out corners)) return;
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

        Quaternion rot = Quaternion.identity;
        //Vector3 pos = new Vector3((float)tvec[0] - screenW / 2f, (float)tvec[1] - screenH / 2f, (float)tvec[2]);
        Vector3 pos = new Vector3((float)tvec[0], (float)tvec[1], (float)tvec[2]);
        pos = cam.transform.rotation * pos;
        pos += cam.transform.position;


        //Vector3 camRealPos = realPos - pos;
        //Vector3 camDiff = camRealPos - cam.transform.position;

        //transform.position = realPos;
        transform.position = pos;

        // rotation only drifts in Y
        Vector3 trackerXZ = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 truthXZ = new Vector3(realPos.x, 0f, realPos.z);
        Vector3 camXZ = new Vector3(cam.transform.position.x, 0f, cam.transform.position.z);
        rot = Quaternion.FromToRotation(truthXZ, trackerXZ);
        transform.rotation = rot;

        tracked = true;
    }
}
