using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using UnityEngine;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.Features2dModule;
using System.IO;
using System.Threading.Tasks;
using System;
using OpenCVForUnity.ImgcodecsModule;

public class Barcode2DMatching
{
    private Mat matOpFlowThis;
    private Mat matOpFlowPrev;
    private int distanceThreshold;
    public Mat fmHomography { get; private set; }
    public bool IsFmProcessing { get; private set; }

    /// <summary>
    /// Constructor to initialize the class with default or custom parameters.
    /// </summary>
    /// <param name="distanceThreshold">Threshold for filtering matches.</param>
    public Barcode2DMatching(int distanceThreshold = 150)
    {
        this.distanceThreshold = distanceThreshold;
        matOpFlowThis = new Mat();
        matOpFlowPrev = new Mat();
        IsFmProcessing = false;
    }

    /// <summary>
    /// Initializes the class for feature matching.
    /// </summary>
    public void Init()
    {
        matOpFlowThis = new Mat();
        matOpFlowPrev = new Mat();
        IsFmProcessing = false;
    }

    /// <summary>
    /// Extracts keypoints and descriptors from the given image.
    /// </summary>
    /// <param name="rgbaMat">Input image in RGBA format.</param>
    /// <returns>Tuple containing keypoints and descriptors.</returns>
    public (MatOfKeyPoint, Mat) GetFeatureMatching(Mat rgbaMat)
    {
        Mat newMat = rgbaMat.clone();
        Imgproc.cvtColor(newMat, newMat, Imgproc.COLOR_RGBA2GRAY);

        ORB sift = ORB.create();
        MatOfKeyPoint keyPoints = new MatOfKeyPoint();
        Mat descriptors = new Mat();
        sift.detectAndCompute(newMat, new Mat(), keyPoints, descriptors);
        Debug.Log("MatOfKeyPoint " + keyPoints.rows());
        return (keyPoints, descriptors);
    }

    /// <summary>
    /// Updates feature matching and returns the corners of the matched region.
    /// </summary>
    /// <param name="referenceImage">Current frame.</param>
    /// <param name="queryImage">Previous frame.</param>
    /// <param name="keypoints">Keypoints of the reference image.</param>
    /// <param name="descriptors">Descriptors of the reference image.</param>
    /// <param name="path">Path to save output images (optional).</param>
    /// <returns>Array of corners as Vector2.</returns>
    public MatOfPoint2f MatchFeaturesAndFindCorners(Mat referenceImage, Mat queryImage, MatOfKeyPoint keypoints, Mat descriptors, string path)
    {
        IsFmProcessing = true;
        MatOfPoint2f corners = ProcessCurrentFrame(referenceImage, queryImage, keypoints, descriptors, path);
        IsFmProcessing = false;
        return corners;
    }

    private MatOfPoint2f ProcessCurrentFrame(Mat referenceImage, Mat queryImage, MatOfKeyPoint keyPoints, Mat descriptors, string path)
    {
        Mat homographyMatrix = new Mat();
        try
        {
            Imgproc.cvtColor(referenceImage, matOpFlowThis, Imgproc.COLOR_RGBA2GRAY);
            ORB sift = ORB.create();
            MatOfKeyPoint keyPointsThis = new MatOfKeyPoint();
            Mat descriptorsThis = new Mat();
            sift.detectAndCompute(matOpFlowThis, new Mat(), keyPointsThis, descriptorsThis);

            BFMatcher bfMatcher = new BFMatcher(BFMatcher.BRUTEFORCE_HAMMING, true);
            MatOfDMatch matches = new MatOfDMatch();
            bfMatcher.match(descriptors, descriptorsThis, matches);

            List<DMatch> goodMatchesList = FilterGoodMatches(matches);

            // DrawAndSaveMatches(referenceImage, queryImage, keyPoints, keyPointsThis, matches, path);

            if (goodMatchesList.Count > 0)
            {
                List<Point> crop = new List<Point>();
                List<Point> scene = new List<Point>();

                foreach (DMatch match in goodMatchesList)
                {
                    crop.Add(keyPoints.toList()[match.queryIdx].pt);
                    scene.Add(keyPointsThis.toList()[match.trainIdx].pt);
                }

                MatOfPoint2f cropMat = new MatOfPoint2f();
                MatOfPoint2f referenceMat = new MatOfPoint2f();
                cropMat.fromList(crop);
                referenceMat.fromList(scene);

                Mat mask = new Mat();
                homographyMatrix = Calib3d.findHomography(cropMat, referenceMat, Calib3d.RANSAC, 5.0, mask);

                int inliers = Core.countNonZero(mask);
                float inlierRatio = (float)inliers / goodMatchesList.Count;
                Debug.Log("Confident FM inlierRatio = " + inlierRatio.ToString());
                if (inlierRatio < 0.8)
                {
                    homographyMatrix = null;
                }
                else
                {
                    MatOfPoint2f objCorners = new MatOfPoint2f(
                        new Point(0, 0),
                        new Point(queryImage.cols(), 0),
                        new Point(queryImage.cols(), queryImage.rows()),
                        new Point(0, queryImage.rows())
                    );

                    MatOfPoint2f trackedImageCornerInReferenceImage = new MatOfPoint2f();
                    Core.perspectiveTransform(objCorners, trackedImageCornerInReferenceImage, homographyMatrix);

                    // Add (referenceImage.width / 2, referenceImage.height / 2) to each point
                    Point[] points = trackedImageCornerInReferenceImage.toArray();
                    for (int i = 0; i < points.Length; i++)
                    {
                        points[i].x += referenceImage.width() / 2.0;
                        points[i].y += referenceImage.height() / 2.0;
                    }
                    trackedImageCornerInReferenceImage.fromArray(points);

                    return trackedImageCornerInReferenceImage;
                }
            }
            else
            {
                Debug.Log("SIFT cannot find homography");
                homographyMatrix = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("ProcessCurrentFrame " + ex.Message);
        }
        return new MatOfPoint2f();
    }

    private List<DMatch> FilterGoodMatches(MatOfDMatch matches)
    {
        List<DMatch> goodMatchesList = new List<DMatch>();
        foreach (var match in matches.toList())
        {
            if (match.distance < distanceThreshold)
            {
                goodMatchesList.Add(match);
            }
        }
        return goodMatchesList;
    }

    private Vector2[] ConvertMatOfPoint2fToVector2Array(MatOfPoint2f matPoints)
    {
        Point[] pointsArray = matPoints.toArray();
        Vector2[] vectorArray = new Vector2[pointsArray.Length];

        for (int i = 0; i < pointsArray.Length; i++)
        {
            vectorArray[i] = new Vector2((float)pointsArray[i].x, (float)pointsArray[i].y);
        }

        return vectorArray;
    }

    /// <summary>
    /// Draws match lines between reference and query images and saves the match image.
    /// </summary>
    /// <param name="referenceImage">Reference image.</param>
    /// <param name="queryImage">Query image.</param>
    /// <param name="keypointsReference">Keypoints of the reference image.</param>
    /// <param name="keypointsQuery">Keypoints of the query image.</param>
    /// <param name="matches">Matches between keypoints.</param>
    /// <param name="outputPath">Path to save the output image.</param>
    public void DrawAndSaveMatches(Mat referenceImage, Mat queryImage, MatOfKeyPoint keypointsReference, MatOfKeyPoint keypointsQuery, MatOfDMatch matches, string outputPath)
    {
        try
        {
            Mat outputImage = new Mat();
            Features2d.drawMatches(referenceImage, keypointsReference, queryImage, keypointsQuery, matches, outputImage);

            // Imgcodecs.imwrite(outputPath, outputImage);
            // Debug.Log("Match image saved at: " + outputPath);
            string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "outputImage.jpg");
            Imgcodecs.imwrite(filePath, outputImage);
            NativeGallery.SaveImageToGallery( filePath, "Solomon", "outputImage.jpg" );
        }
        catch (Exception ex)
        {
            Debug.LogError("Error in DrawAndSaveMatches: " + ex.Message);
        }
    }
}