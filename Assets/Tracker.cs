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
    //public GameObject TrackerMesh;

    public bool tracked;


    // physical screen size in meters
    private float screenW = 0.15f;
    private float screenH = 0.075f;

    private Quaternion orientationOffset = Quaternion.Euler(180f, 0f, 0f);

    private Vector3 rawpos;
    private Vector3 worldpos;

    public Vector3 GetRawPosition()
    {
        return rawpos;
    }

    public Vector3 GetWorldPosition()
    {
        return worldpos;
    }

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
        //TrackerMesh.SetActive(tracked);
        // standard OpenCV algorithm to find trackers
        // finds tracker pose in frame of camera
        Point2f[] corners;
        tracked = false;
        if (!detector.GetTrackerCorners(id, out corners))
        {
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
        rawpos = new Vector3((float)tvec[0], (float)tvec[1], (float)tvec[2]);
        worldpos = rawpos;
        rawpos = cam.transform.rotation * worldpos;
        worldpos = rawpos + cam.transform.position;

        transform.position = worldpos;
        
        tracked = true;
    }
}
