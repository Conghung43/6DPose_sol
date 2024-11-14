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

public class QRCodeReader : MonoBehaviour
{
    //public RawImage cameraDisplay;
    //private WebCamTexture camTexture;
    private IBarcodeReader barcodeReader;

    // Input Parameters
    private float qrCodeSize = 0.081f; // QR code's physical size (e.g., 10 cm)
    private Vector2 focalLength = new Vector2(800, 800); // Focal length in pixels
    private Vector2 principalPoint = Vector2.zero; // Principal point in pixels
    private Vector2[] qrCodeCorners2D = new Vector2[4];
    private Vector2[] qrCodePositionPattern = new Vector2[] { new Vector2(0,26),
                                                                new Vector2(0,0),
                                                                new Vector2(26,0),
                                                                new Vector2(23,23)};
    public GameObject transfromOrigin;
    public GameObject arCamera;
    private int count = 0;

    void Start()
    {
        barcodeReader = new BarcodeReader();
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
                Color32[] colorByte = TrackedImageInfoManager.cpuImageTexture.GetPixels32();//
#else
                //string inputFilePath = Path.Combine(Application.persistentDataPath, "test.jpg");
                //File.WriteAllBytes(inputFilePath, TrackedImageInfoManager.cpuImageTexture.EncodeToJPG());
                Color32[] colorByte = TrackedImageInfoManager.cpuImageTexture.GetPixels32();
#endif
                var result = barcodeReader.Decode(colorByte, TrackedImageInfoManager.cpuImageTexture.width, TrackedImageInfoManager.cpuImageTexture.height);
                if (result != null)
                {
                    //Debug.Log("QR Code detected: " + result.Text);
                    qrCodeCorners2D[0] = new Vector2((int)result.ResultPoints[0].X, (int)result.ResultPoints[0].Y);
                    qrCodeCorners2D[1] = new Vector2((int)result.ResultPoints[1].X, (int)result.ResultPoints[1].Y);
                    qrCodeCorners2D[2] = new Vector2((int)result.ResultPoints[2].X, (int)result.ResultPoints[2].Y);
                    qrCodeCorners2D[3] = new Vector2((int)result.ResultPoints[3].X, (int)result.ResultPoints[3].Y);

                    FindQRcodeTransformMatrix();
                    // Do something with the decoded QR code here
                }
            }
            catch (System.Exception ex)
            {
                //Debug.Log("QR" + ex.Message);
            }
        }
    }

    private Mat FindHomographyMatrix(Vector2[] src, Vector2[] dst)
    {
        // Define the first set of points (source points)
        List<Point> srcPoints = new List<Point>
    {
        new Point(src[0][0], src[0][1]),  // bottom-left
        new Point(src[1][0], src[1][1]),  // top-left
        new Point(src[2][0], src[2][1]),  // top-right
        new Point(src[3][0], src[3][1])   // bottom-right
    };

        // Define the second set of points (destination points)
        List<Point> dstPoints = new List<Point>
    {
        new Point(dst[0][0], dst[0][1]),  // bottom-left
        new Point(dst[1][0], dst[1][1]),  // top-left
        new Point(dst[2][0], dst[2][1]),  // top-right
        new Point(dst[3][0], dst[3][1])   // bottom-right
    };

        // Convert lists to MatOfPoint2f (required by findHomography)
        MatOfPoint2f srcMat = new MatOfPoint2f();
        srcMat.fromList(srcPoints);

        MatOfPoint2f dstMat = new MatOfPoint2f();
        dstMat.fromList(dstPoints);

        // Calculate the homography matrix
        Mat homography = Calib3d.findHomography(srcMat, dstMat);
        return homography;
    }


    // Function to map a point from destination to source plane using inverse homography
    Point GetPointInSourcePlane(Point dstPoint, Mat homography)
    {
        // Invert the homography matrix
        Mat invHomography = new Mat();
        Core.invert(homography, invHomography);

        // Convert the destination point to Mat format
        MatOfPoint2f dstMat = new MatOfPoint2f(dstPoint);
        MatOfPoint2f srcMat = new MatOfPoint2f();

        // Apply the inverse homography matrix to find the source point
        Core.perspectiveTransform(dstMat, srcMat, invHomography);

        // Return the mapped source point
        return srcMat.toArray()[0];
    }

    private void FindQRcodeTransformMatrix()
    {
        //Step 0: replace alignment pattern with position pattern
        Mat homographyMatrix = FindHomographyMatrix(qrCodeCorners2D, qrCodePositionPattern);

        Point bottomRightPatternCorner = GetPointInSourcePlane(new Point(26, 26), homographyMatrix);

        // Step 1: Define the 3D points of the QR code in the world coordinate system
        MatOfPoint3f qrCodeCorners3D = new MatOfPoint3f(
            new Point3(-qrCodeSize / 2, -qrCodeSize / 2, 0),  // Bottom-left
            new Point3(qrCodeSize / 2, -qrCodeSize / 2, 0),   // Bottom-right or QR code alignment patterns
            new Point3(qrCodeSize / 2, qrCodeSize / 2, 0),    // Top-right
            new Point3(-qrCodeSize / 2, qrCodeSize / 2, 0)    // Top-left
        );

        // Step 2: Convert the 2D corner points into OpenCV format (MatOfPoint2f)
        MatOfPoint2f qrCodeCorners2DMat = new MatOfPoint2f(
            new Point(qrCodeCorners2D[0].x, qrCodeCorners2D[0].y),
            bottomRightPatternCorner,
            new Point(qrCodeCorners2D[2].x, qrCodeCorners2D[2].y),
            new Point(qrCodeCorners2D[1].x, qrCodeCorners2D[1].y)
        );

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
