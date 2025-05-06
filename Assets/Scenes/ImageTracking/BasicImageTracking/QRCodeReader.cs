using UnityEngine;
using UnityEngine.UI;
using ZXing;
using UnityEngine.XR.ARFoundation.Samples;
using System.IO;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using Mediapipe;
using Unity.Barracuda;
using OpenCVForUnity.ImgprocModule;
using System.Collections.Generic;
using OpenCVForUnity.ImgcodecsModule;
using static UnityEngine.XR.ARFoundation.Samples.DynamicLibrary;
using UnityEngine.Networking;
using System.Collections;
using OpenCVForUnity.UnityUtils;

public class QRCodeReader : MonoBehaviour
{
    //public RawImage cameraDisplay;
    //private WebCamTexture camTexture;
    // private IBarcodeReader barcodeReader;

    // Input Parameters
    private float physicImageWidth = 0.325f; // QR code's physical size (e.g., 10 cm)
    private float physicImageHeight = 0.1625f;
    private Vector2 focalLength = new Vector2(800, 800); // Focal length in pixels
    private Vector2 principalPoint = Vector2.zero; // Principal point in pixels
    private MatOfPoint2f qrCodeCorners2D = new MatOfPoint2f();
    public GameObject transfromOrigin;
    public GameObject arCamera;
    private int count = 0;
    public Mat queryImage;
    MatOfKeyPoint keypoints = new MatOfKeyPoint(); 
    Mat descriptors = new Mat();
    Barcode2DMatching barcode2DMatching;
    public RawImage targetImage;
    private string fileName = "queryImage.jpg"; // Name of the image file in StreamingAssets
    void Start()
    {
        // Texture2D texture = new Texture2D(rawImage.texture.width, rawImage.texture.height, TextureFormat.RGBA32, false);
        StartCoroutine(LoadImage());
    }

    IEnumerator LoadImage()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        byte[] imgData;

        //Check if we should use UnityWebRequest or File.ReadAllBytes
        if (path.Contains("://") || path.Contains(":///"))
        {
            Debug.Log("Load image success 1");
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(path);
            yield return request.SendWebRequest();
            Debug.Log("Load image success 2");
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("load image error: " + request.error);
            }
            else
            {
                Debug.Log("Load image success");
                Texture2D texture = DownloadHandlerTexture.GetContent(request); 
                //Load raw Data into Texture2D 
                targetImage.texture = texture;
                if (queryImage == null)
                {
                    queryImage = new Mat(texture.height, texture.width, CvType.CV_8UC4);
                }
                Utils.texture2DToMat(texture, queryImage);
                Imgproc.cvtColor(queryImage, queryImage, Imgproc.COLOR_RGB2BGR);
                // imwrite queryImage.jpg 
                // Imgcodecs.imwrite("test.jpg", queryImage);
                Debug.Log("Loaded Mat color at Start (non-Android)");

                barcode2DMatching = new Barcode2DMatching(150);
                barcode2DMatching.Init();
                (keypoints, descriptors) = barcode2DMatching.GetFeatureMatching(queryImage);
            }
        }
        else
        {
            Debug.Log("Load image else");
            Texture2D texture = new Texture2D(2, 2);
            imgData = File.ReadAllBytes(path);
            //Load raw Data into Texture2D 
            texture.LoadImage(imgData);
            targetImage.texture = texture;
            if (queryImage == null)
            {
                queryImage = new Mat(texture.height, texture.width, CvType.CV_8UC4);
            }
            Utils.texture2DToMat(texture, queryImage);
            Imgproc.cvtColor(queryImage, queryImage, Imgproc.COLOR_RGB2BGR);
            // imwrite queryImage.jpg 
            // Imgcodecs.imwrite("test.jpg", queryImage);
            Debug.Log("Loaded Mat color at Start (non-Android)");

            barcode2DMatching = new Barcode2DMatching(150);
            barcode2DMatching.Init();
            (keypoints, descriptors) = barcode2DMatching.GetFeatureMatching(queryImage);
        }

        
    }
    void Update()
    {
        if (principalPoint == Vector2.zero)
        {
            focalLength = TrackedImageInfoManager.intrinsics.focalLength;
            principalPoint = TrackedImageInfoManager.intrinsics.principalPoint;
            Debug.Log("QR Code detected: " + focalLength.ToString());
        }

        if (TrackedImageInfoManager.cpuImageTexture != null)
        {
            try
            {
#if UNITY_EDITOR
                //File.WriteAllBytes("test.jpg", TrackedImageInfoManager.cpuImageTexture.EncodeToJPG());
                focalLength = new Vector2(936.2321683838078f, 936.1081714012856f);
                principalPoint = new Vector2(959.2009481268866f, 538.9017422822632f);
                // Color32[] colorByte = TrackedImageInfoManager.cpuImageTexture.GetPixels32();//
#else
                //string inputFilePath = Path.Combine(Application.persistentDataPath, "test.jpg");
                //File.WriteAllBytes(inputFilePath, TrackedImageInfoManager.cpuImageTexture.EncodeToJPG());
                // Color32[] colorByte = TrackedImageInfoManager.cpuImageTexture.GetPixels32();
                // Create a Mat with the same size as the texture
#endif
                Mat referenceMat = new Mat(TrackedImageInfoManager.cpuImageTexture.height, TrackedImageInfoManager.cpuImageTexture.width, CvType.CV_8UC4);
                // rawImage.texture = TrackedImageInfoManager.cpuImageTexture;
                // Convert Texture2D to Mat
                Utils.texture2DToMat(TrackedImageInfoManager.cpuImageTexture, referenceMat);
                // var result = barcodeReader.Decode(colorByte, TrackedImageInfoManager.cpuImageTexture.width, TrackedImageInfoManager.cpuImageTexture.height);
                
                referenceMat = new Mat(referenceMat, 
                    new OpenCVForUnity.CoreModule.Rect(
                        (int)(referenceMat.width()/4),
                        (int)(referenceMat.height()/4),
                        (int)(referenceMat.width()/2),
                        (int)(referenceMat.height()/2)));
                
                qrCodeCorners2D = barcode2DMatching.MatchFeaturesAndFindCorners(referenceMat, queryImage, keypoints, descriptors, "test.jpg");
                if (qrCodeCorners2D.toArray().Length > 0)
                {
                    FindQRcodeTransformMatrix(qrCodeCorners2D);
                    // Do something with the decoded QR code here
                }
            }
            catch (System.Exception ex)
            {
                //Debug.Log("QR" + ex.Message);
            }
        }
    }
    private void FindQRcodeTransformMatrix(MatOfPoint2f qrCodeCorners2DMat)
    {
        //Step 0: replace alignment pattern with position pattern
        // Mat homographyMatrix = FindHomographyMatrix(qrCodeCorners2D, qrCodePositionPattern);

        // Point bottomRightPatternCorner = GetPointInSourcePlane(new Point(26, 26), homographyMatrix);

        // Step 1: Define the 3D points of the QR code in the world coordinate system
        MatOfPoint3f qrCodeCorners3D = new MatOfPoint3f(
            new Point3(-physicImageWidth / 2, -physicImageHeight / 2, 0),  // Bottom-left
            new Point3(physicImageWidth / 2, -physicImageHeight / 2, 0),   // Bottom-right or QR code alignment patterns
            new Point3(physicImageWidth / 2, physicImageHeight / 2, 0),    // Top-right
            new Point3(-physicImageWidth / 2, physicImageHeight / 2, 0)    // Top-left
        );

        // // Step 2: Convert the 2D corner points into OpenCV format (MatOfPoint2f)
        // MatOfPoint2f qrCodeCorners2DMat = new MatOfPoint2f(
        //     new Point(qrCodeCorners2D[0].x, qrCodeCorners2D[0].y),
        //     bottomRightPatternCorner,
        //     new Point(qrCodeCorners2D[2].x, qrCodeCorners2D[2].y),
        //     new Point(qrCodeCorners2D[1].x, qrCodeCorners2D[1].y)
        // );



        // Step 3: Define the camera intrinsic matrix
        Mat cameraMatrix = new Mat(3, 3, CvType.CV_64F);
        cameraMatrix.put(0, 0, focalLength.x); cameraMatrix.put(0, 2, principalPoint.x);
        cameraMatrix.put(1, 1, focalLength.y); cameraMatrix.put(1, 2, principalPoint.y);
        cameraMatrix.put(2, 2, 1.0);

        // Step 4: Define distortion coefficients (assuming zero distortion)
        MatOfDouble distCoeffs = new MatOfDouble(0, 0, 0, 0);

        // Step 5: Solve for rotation and translation vectors
        Mat rvec = new Mat();
        Mat tvec = new Mat();
        Calib3d.solvePnP(qrCodeCorners3D, qrCodeCorners2DMat, cameraMatrix, distCoeffs, rvec, tvec);

        // Step 6: Convert rotation vector to rotation matrix
        Mat rotationMatrix = new Mat();
        Calib3d.Rodrigues(rvec, rotationMatrix);

        // Step 7: Construct the 4x4 transformation matrix
        Matrix4x4 qrCodeToCameraTransformMatrix = Matrix4x4.identity;
        qrCodeToCameraTransformMatrix.m00 = (float)rotationMatrix.get(0, 0)[0];
        qrCodeToCameraTransformMatrix.m01 = (float)rotationMatrix.get(0, 1)[0];
        qrCodeToCameraTransformMatrix.m02 = (float)rotationMatrix.get(0, 2)[0];
        qrCodeToCameraTransformMatrix.m10 = (float)rotationMatrix.get(1, 0)[0];
        qrCodeToCameraTransformMatrix.m11 = (float)rotationMatrix.get(1, 1)[0];
        qrCodeToCameraTransformMatrix.m12 = (float)rotationMatrix.get(1, 2)[0];
        qrCodeToCameraTransformMatrix.m20 = (float)rotationMatrix.get(2, 0)[0];
        qrCodeToCameraTransformMatrix.m21 = (float)rotationMatrix.get(2, 1)[0];
        qrCodeToCameraTransformMatrix.m22 = (float)rotationMatrix.get(2, 2)[0];

        qrCodeToCameraTransformMatrix.m03 = (float)tvec.get(0, 0)[0];
        qrCodeToCameraTransformMatrix.m13 = (float)tvec.get(1, 0)[0];
        qrCodeToCameraTransformMatrix.m23 = (float)tvec.get(2, 0)[0];

        Vector3 pos = qrCodeToCameraTransformMatrix.GetColumn(3);
        Quaternion ros = Quaternion.LookRotation(
            qrCodeToCameraTransformMatrix.GetColumn(2),
            qrCodeToCameraTransformMatrix.GetColumn(1)
        );

        (ros, pos) = Inference.ConvertToOppositeHandedness(ros, pos);

        qrCodeToCameraTransformMatrix = Matrix4x4.TRS(pos, ros, Vector3.one);//;

        // Multiply transformMatrix by camera's world-to-local matrix inverse
        Matrix4x4 qrCodeToWorldTransformMatrix =  Camera.main.transform.localToWorldMatrix * qrCodeToCameraTransformMatrix;

        // Extract the position, rotation, and scale from gameObjectWorldTransformMatrix
        Vector3 position;// = qrCodeToWorldTransformMatrix.GetColumn(3);
        Quaternion rotation;// = Quaternion.LookRotation(

        Inference.MatrixToQuaternionTranslation(qrCodeToWorldTransformMatrix, out rotation, out position);

        transfromOrigin.transform.position = position;
        transfromOrigin.transform.rotation = rotation;
    }
}
