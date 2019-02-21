namespace GoogleARCore
{

    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine.UI;
    using UnityEngine;
    using System.IO;
    using OpenCvSharp;
    using System;

    public class CameraCalibrationManager : MonoBehaviour
    {

        public string filename = "/camera_mat.txt";
        public float checkerSize = 25.4f;
        public Size checkerDim = new Size(9, 6);
        private double[,] cameraMat;
        private double[] distortion;

        public Image OutImage;
        private Texture2D OutTexture;
        private double error_threshold = 0.5;

        private List<MatOfPoint2f> imagePoints = new List<MatOfPoint2f>();
        private List<MatOfPoint3f> objectPoints = new List<MatOfPoint3f>();

        private int resW;
        private int resH;

        public bool allowCalibration = true;

        float xFocus, yFocus, xOffset, yOffset;
        public float minXFocus = 0;
        public float minYFocus = 0;
        public float maxXFocus = 600;
        public float maxYFocus = 600;

        bool calibrating = false;

        // Use this for initialization
        void Start()
        {
            cameraMat = new double[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    cameraMat[i, j] = (i == j ? 1 : 0);
                }
            }
            distortion = new double[8];
            List<float> values = new List<float>();

            bool okay = true;
            if (File.Exists(Application.persistentDataPath + filename))
            {
                string[] lines = File.ReadAllLines(Application.persistentDataPath + filename);
                if (lines.Length == 17)
                {
                    foreach (string line in lines)
                    {
                        float val;
                        if (float.TryParse(line, out val))
                        {
                            values.Add(val);
                        }
                        else
                        {
                            okay = false;
                        }
                    }
                }
                else
                {
                    okay = false;
                }
            }
            else
            {
                okay = false;
                saveMatrix();
            }
            if (okay)
            {
                for (int i = 0; i < 9; i++)
                {
                    cameraMat[(int)Mathf.Floor(i / 3), i % 3] = values[i];
                }
                for (int i = 9; i < 17; i++)
                {
                    distortion[i - 9] = values[i];
                }
            }
            xFocus = (float)cameraMat[0, 0];
            yFocus = (float)cameraMat[1, 1];
            xOffset = (float)cameraMat[0, 2];
            yOffset = (float)cameraMat[1, 2];
            Debug.Log(string.Format("UNITY CAMMAT: fx {0} fy {1} cx {2} cy {3}", cameraMat[0, 0], cameraMat[1, 1], cameraMat[0, 2], cameraMat[1, 2]));
        }

        void OnGUI()
        {
            //The Labels show what the Sliders represent
            GUIStyle labelstyle = new GUIStyle(GUI.skin.button);
            labelstyle.fontSize = 46;
            GUIStyle buttonstyle = new GUIStyle(GUI.skin.button);
            buttonstyle.fontSize = 46;

            GUIStyle sliderstyle = new GUIStyle(GUI.skin.horizontalSlider);
            sliderstyle.fixedHeight = 50;
            sliderstyle.padding = new RectOffset(0, 0, -75, 0);
            GUIStyle thumbstyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
            thumbstyle.fixedHeight = 200;
            thumbstyle.fixedWidth = 50;

            float padding = 50;
            float buttonX = 50;
            float buttonW = 300;
            float buttonH = 300;
            float labelX = buttonX + buttonW + padding;
            float labelW = 400;
            float labelH = 100;
            float sliderX = labelX + labelW + padding;
            float sliderW = 1200;
            float sliderH = 300;
            float label2X = sliderX + sliderW + padding;

            float row1Y = padding;
            float rowH = buttonH;
            float row2Y = row1Y + rowH + padding;
            float row3Y = row2Y + rowH + padding;
            float row4Y = row3Y + rowH + padding;
            float labelYOffset = (sliderH - labelH) / 2;
            float sliderYOffset = (sliderH - sliderstyle.fixedHeight) / 2;

            if (calibrating)
            {
                GUI.Label(new UnityEngine.Rect(labelX, row1Y + labelYOffset, labelW, labelH), "Focus X : ", labelstyle);
                GUI.Label(new UnityEngine.Rect(labelX, row2Y + labelYOffset, labelW, labelH), "Focus Y : ", labelstyle);
                GUI.Label(new UnityEngine.Rect(label2X, row1Y + labelYOffset, labelW, labelH), cameraMat[0, 0].ToString(), labelstyle);
                GUI.Label(new UnityEngine.Rect(label2X, row2Y + labelYOffset, labelW, labelH), cameraMat[1, 1].ToString(), labelstyle);
                GUI.Label(new UnityEngine.Rect(labelX, row3Y + labelYOffset, labelW, labelH), "Offset X : ", labelstyle);
                GUI.Label(new UnityEngine.Rect(labelX, row4Y + labelYOffset, labelW, labelH), "Offset Y : ", labelstyle);
                GUI.Label(new UnityEngine.Rect(label2X, row3Y + labelYOffset, labelW, labelH), cameraMat[0, 2].ToString(), labelstyle);
                GUI.Label(new UnityEngine.Rect(label2X, row4Y + labelYOffset, labelW, labelH), cameraMat[1, 2].ToString(), labelstyle);

                //Create a horizontal Slider that controls the x and y Positions of the anchors
                xFocus = GUI.HorizontalSlider(new UnityEngine.Rect(sliderX, row1Y + sliderYOffset, sliderW, sliderH), xFocus, minXFocus, maxXFocus, sliderstyle, thumbstyle);
                yFocus = GUI.HorizontalSlider(new UnityEngine.Rect(sliderX, row2Y + sliderYOffset, sliderW, sliderH), yFocus, minYFocus, maxYFocus, sliderstyle, thumbstyle);
                xOffset = GUI.HorizontalSlider(new UnityEngine.Rect(sliderX, row3Y + sliderYOffset, sliderW, sliderH), xOffset, minXFocus, maxXFocus, sliderstyle, thumbstyle);
                yOffset = GUI.HorizontalSlider(new UnityEngine.Rect(sliderX, row4Y + sliderYOffset, sliderW, sliderH), yOffset, minYFocus, maxYFocus, sliderstyle, thumbstyle);

                //Detect a change in the GUI Slider
                if (GUI.changed)
                {
                    //Change the RectTransform's anchored positions depending on the Slider values
                    cameraMat[0, 0] = xFocus;
                    cameraMat[1, 1] = yFocus;
                    cameraMat[0, 2] = xOffset;
                    cameraMat[1, 2] = yOffset;
                }
            }

            if (GUI.Button(new UnityEngine.Rect(buttonX, row1Y, buttonW, buttonH), calibrating ? "Save" : "Calibrate", buttonstyle))
            {
                calibrating = !calibrating;
                if (calibrating)
                {
                    saveMatrix();
                }
            }
        }

        public double[,] getCameraMat()
        {
            return cameraMat;
        }
        public double[] getDistCoeffs()
        {
            return distortion;
        }

        private void saveMatrix ()
        {
            string[] lines = new string[17];
            for (int j = 0; j < 3; j++)
            {
                for (int i = 0; i < 3; i++)
                {
                    lines[j + i * 3] = cameraMat[i, j].ToString();
                }
            }
            for(int i = 9; i < 17; i++)
            {
                lines[i] = distortion[i - 9].ToString();
            }
            File.WriteAllLines(Application.persistentDataPath + filename, lines);
        }

        private double calibrate(int width, int height)
        {
            if (imagePoints.ToArray().Length == 0)
            {
                return -1;
            }
            Vec3d[] rvecs, tvecs;

            double error = Cv2.CalibrateCamera(objectPoints.ToArray(), imagePoints.ToArray(), new Size(width, height), cameraMat, distortion, out rvecs, out tvecs);
            Debug.Log("UNITY CALIBRATION: " + error);
            return error;
        }

        private void collectPoints(TextureReader.TextureReaderApi.ImageFormatType format, int width, int height, IntPtr pixelBuffer)
        {
            List<Point3f> objectPoint = new List<Point3f>();
            for (int i = 0; i < checkerDim.Height; i++)
            {
                for (int j = 0; j < checkerDim.Width; j++)
                {
                    objectPoint.Add(new Point3f(j * checkerSize, i * checkerSize, 0f));
                }
            }
            Mat im = new Mat(height, width, MatType.CV_8UC1, pixelBuffer);

            MatOfPoint2f corners = new MatOfPoint2f();
            bool found = Cv2.FindChessboardCorners(im, checkerDim, OutputArray.Create(corners));
            
            if (!found)
            {
                //Destroy(OutTexture);
                //OutTexture = Unity.MatToTexture(im);
                //OutImage.sprite = Sprite.Create(OutTexture, new UnityEngine.Rect(Vector2.zero, new Vector2(width, height)), Vector2.zero);
                im.Dispose();
                return;
            }
            imagePoints.Add(corners);
            MatOfPoint3f objectPointMat = new MatOfPoint3f(checkerDim.Width * checkerDim.Height, 1, objectPoint.ToArray());
            objectPoints.Add(objectPointMat);

            if (corners.ToArray().Length > 0)
            {
                
                //Cv2.DrawChessboardCorners(im, checkerDim, (InputArray)corners, true);
                //Destroy(OutTexture);
                //OutTexture = Unity.MatToTexture(im);
                //OutImage.sprite = Sprite.Create(OutTexture, new UnityEngine.Rect(Vector2.zero, new Vector2(width, height)), Vector2.zero);
            }
            im.Dispose();

            Debug.Log("UNITY SAMPLE ACQUIRED");
        }



        // Update is called once per frame
        void Update () {
            if (!allowCalibration) return;
            Touch touch;
            if (Input.touchCount < 1)
            {
                return;
            }
            touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended)
            {
                if (resW == 0 || resH == 0) return;
                double error = calibrate(resW, resH);
                if (error < error_threshold && error > 0)
                {
                    saveMatrix();
                }
                imagePoints = new List<MatOfPoint2f>();
                objectPoints = new List<MatOfPoint3f>();
                return;
            }
            using (var image = Frame.CameraImage.AcquireCameraImageBytes())
            {
                if (!image.IsAvailable || touch.position.y > 360 || touch.position.x < Screen.width - 360)
                {
                    return;
                }
                resW = image.Width;
                resH = image.Height;
                collectPoints(TextureReader.TextureReaderApi.ImageFormatType.ImageFormatColor, image.Width, image.Height, image.Y);
                
            }
        }
        
    }
}
