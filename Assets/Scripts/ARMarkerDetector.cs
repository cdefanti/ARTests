namespace GoogleARCore
{

    using UnityEngine;
    using System.Collections;
    using UnityEngine.UI;
    using OpenCvSharp;
    using OpenCvSharp.Aruco;
    using System.Threading;
    using System;
    using System.Collections.Generic;

    public class ARMarkerDetector : MonoBehaviour
    {
        //public Image outImage;
        //public Text text;

        private Dictionary dictionary;
        private DetectorParameters detectorParameters;
        //private Texture2D outTexture;

        private Dictionary<int, Point2f[]> trackers;

        void Start()
        {
            trackers = new Dictionary<int, Point2f[]>();

            // Create default parameres for detection
            detectorParameters = DetectorParameters.Create();

            // Dictionary holds set of all available markers
            dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.DictArucoOriginal);
        }

        public bool GetTrackerCorners(int id, out Point2f[] output)
        {
            if (trackers.ContainsKey(id))
            {
                output = trackers[id];
                return true;
            }
            output = new Point2f[0];
            return false;
        }

        private void _OnImageAvailable(TextureReader.TextureReaderApi.ImageFormatType format, int width, int height, IntPtr pixelBuffer, int bufferSize)
        {
            // Variables to hold results
            Point2f[][] corners;
            int[] ids;
            Point2f[][] rejectedImgPoints;
            // Create Opencv image from unity texture
            Mat mat = new Mat(height, width, MatType.CV_8UC1, pixelBuffer);
            Size s = mat.Size();
            if (s.Width > 0 && s.Height > 0)
            {
                trackers = new Dictionary<int, Point2f[]>();
                Size half = new Size(width / 2, height / 2);
                //mat.Resize(half);
                // Detect and draw markers
                
                CvAruco.DetectMarkers(mat, dictionary, out corners, out ids, detectorParameters, out rejectedImgPoints);

                //CvAruco.DrawDetectedMarkers(mat, corners, ids);
                //mat.Resize(s);

                // Create Unity output texture with detected markers
                //Destroy(outTexture);
                //outTexture = Unity.MatToTexture(mat);
                //outImage.sprite = Sprite.Create(outTexture, new UnityEngine.Rect(Vector2.zero, new Vector2(width, height)), Vector2.zero);
                for (int i = 0; i < ids.Length; i++)
                {
                    for (int j = 0; j < corners[i].Length; j++)
                    {
                        corners[i][j].Y = height - corners[i][j].Y;
                    }
                    trackers[ids[i]] = corners[i];
                }
            }
            
            mat.Dispose();
            //float dt = Time.deltaTime;
            //text.text = string.Format("FPS: {0}", 1f / dt);
        }

        private void Update()
        {
            using (var image = Frame.CameraImage.AcquireCameraImageBytes())
            {
                if (!image.IsAvailable)
                {
                    return;
                }
                _OnImageAvailable(TextureReader.TextureReaderApi.ImageFormatType.ImageFormatGrayscale,
                    image.Width, image.Height, image.Y, 0);
            }
        }
    }
}