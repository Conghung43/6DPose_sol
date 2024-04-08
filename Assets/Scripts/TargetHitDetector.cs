using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class TargetHitDetector : MonoBehaviour
{
    private Vector3 checkpointPosition;

    public GameObject targetObject; // The specific game object you want to check for hits.
    public BoxCollider targetObjectBoxCollider;
    public Camera arCamera;
    public MeshRenderer targetMeshRender;

    private Vector3[] topCorners;
    public static Vector3[] lineBasedPoints = new Vector3[4];
    public GameObject testObject0;
    public GameObject testObject1;
    [SerializeField] private TMPro.TextMeshProUGUI logInfo;
    Vector3 startPoint;
    // Define a delegate type for the event (no parameters)
    //public delegate void CheckpointUpdateEvent();

    // Declare an event of the delegate type
    //public static event CheckpointUpdateEvent OnCheckpointUpdateEvent;

    void Update()
    {
        //logInfo.text = "Update";
        if (StationStageIndex.FunctionIndex != "Sample")
        {
            return;
        }
        topCorners = FindObjectCorner();
        checkpointPosition = StationStageIndex.stagePosition;//StationStageIndex.gameObjectPoints[$"{StationStageIndex.stageIndex}"][0];
                                                                // Update position for next phase;
                                                                //OnCheckpointUpdateEvent?.Invoke();
        EventManager.OnCheckpointUpdateEvent?.Invoke(this, new EventManager.OnCheckpointUpdateEventArgs
        {
            status = true
        });
        startPoint = arCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 4, 0.3f));
        testObject0.transform.position = startPoint;
        testObject1.transform.position = arCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 0.5f));
            //logInfo.text = arCamera.WorldToScreenPoint(lineBasedPoints[3]).ToString() + Profiler.GetMonoUsedSizeLong().ToString();

        //logInfo.text = startPoint.ToString();
        Vector3 checkpointClosetPointOnCube = FindCheckpointClosestPointOnCubeObject(checkpointPosition);
        //testObject1.transform.position = topCorners[1];
        Vector4 planeContainCheckpointClosetPoint = FindPlaneEquation(checkpointClosetPointOnCube, checkpointPosition - checkpointClosetPointOnCube);
        Vector3[] pointsOnPlane = { arCamera.transform.position };
        List<Vector3> mileStoneClasify = ClasifyPathPoints(planeContainCheckpointClosetPoint, pointsOnPlane, targetObject.transform.position);
        
        if (mileStoneClasify.Count == 1)
        {
            lineBasedPoints[0] = startPoint;
            for (int i = 1; i < 3; i++)
            {
                lineBasedPoints[i] = checkpointPosition;
            }
            //Debug.Log(startPoint.ToString());
        }
        else
        {
            Vector4 CameraCheckpointVerticalPlane = FindVerticalPlane(checkpointPosition, arCamera.transform.position);
            Vector3 horizontalPlaneNormalVector = Vector3.Cross(CameraCheckpointVerticalPlane, startPoint - checkpointPosition);
            Vector4 CameraCheckpointHorizontalPlane = FindPlaneEquation(startPoint, horizontalPlaneNormalVector);
            Vector3[] mileStoneCorner = FindMileStoneList(CameraCheckpointHorizontalPlane);
            mileStoneClasify = ClasifyPathPoints(CameraCheckpointVerticalPlane, mileStoneCorner, targetObject.transform.position);
            GenerateLineBasedPoint(mileStoneClasify, startPoint);
        }
    }

    void GenerateLineBasedPoint(List<Vector3> basedPoints, Vector3 startPoint)
    {
        lineBasedPoints[0] = startPoint;
        if (basedPoints.Count == 0)
        {
            lineBasedPoints[1] = lineBasedPoints[2] = checkpointPosition;
        }
        else if (basedPoints.Count == 1)
        {
            lineBasedPoints[1] = basedPoints[0];
            lineBasedPoints[2] = checkpointPosition;
            //testObject0.transform.position = basedPoints[0];
        }
        else if (basedPoints.Count == 2)
        {
            Vector4 basedPointsVerticalPlane = FindVerticalPlane(basedPoints[0], basedPoints[1]);
            Vector3[] checklist = new Vector3[1];
            checklist[0] = startPoint;
            List<Vector3> mileStoneClasify = ClasifyPathPoints(basedPointsVerticalPlane, checklist, checkpointPosition);
            if (mileStoneClasify.Count == 1)
            {
                lineBasedPoints[1] = basedPoints[1];
                lineBasedPoints[2] = checkpointPosition;
                //testObject0.transform.position = basedPoints[1];
            }
            else
            {
                if (Vector3.Distance(basedPoints[1], checkpointPosition)< Vector3.Distance(basedPoints[0], checkpointPosition))
                {
                    lineBasedPoints[1] = basedPoints[0];
                    lineBasedPoints[2] = basedPoints[1];
                }
                else
                {
                    lineBasedPoints[1] = basedPoints[1];
                    lineBasedPoints[2] = basedPoints[0];
                }
                //testObject0.transform.position = basedPoints[0];
                //testObject1.transform.position = basedPoints[1];
            }
        }
        lineBasedPoints[3] = checkpointPosition;
    }


    Vector3[] FindMileStoneList(Vector4 Plane)
    {
        Vector3[] corners = new Vector3[4];
        for (int i = 0; i < corners.Length; i++)
        {
            float yValue = (-Plane.w - topCorners[i].x * Plane.x - topCorners[i].z * Plane.z) / Plane.y;
            corners[i] = new Vector3(topCorners[i].x, yValue, topCorners[i].z);
        }
        return corners;
    }

    Vector4 FindHorizontalPlaneContainCameraCheckpoint(Vector3 startPoint)
    {
        // Calculate the direction vector of the line AB
        Vector3 direction = checkpointPosition - startPoint;

        // Find a perpendicular vector in the x-z plane
        Vector3 perpendicular = new Vector3(direction.z, 0f, -direction.x).normalized;
        perpendicular = Vector3.Cross(direction, perpendicular);
        return new Vector4(perpendicular.x, perpendicular.y, perpendicular.z, -Vector3.Dot(perpendicular, checkpointPosition));
    }

    Vector4 FindVerticalPlane(Vector3 point1, Vector3 point2)
    {
        // Calculate the direction vector of the line AB
        Vector3 direction = point1 - point2;

        // Find a perpendicular vector in the x-z plane
        Vector3 perpendicular = new Vector3(0, 1f, 0).normalized;
        perpendicular = Vector3.Cross(direction, perpendicular);
        return new Vector4(perpendicular.x, perpendicular.y, perpendicular.z, -Vector3.Dot(perpendicular, point1));
    }
    List<Vector3> ClasifyPathPoints(Vector4 verticalPlane, Vector3[] points, Vector3 targetPosition)
    {
        bool centerSide = PointInPlaneEquation(verticalPlane, targetPosition) > 0;
        //Debug.Log(centerSide + verticalPlane.ToString());
        bool pointSide;
        List<Vector3> mileStonePoint = new List<Vector3>();
        for (int i = 0; i < points.Length; i++)
        {
            pointSide = PointInPlaneEquation(verticalPlane, points[i]) > 0;
            if (pointSide != centerSide)
            {
                mileStonePoint.Add(points[i]);
            }
        }
        return mileStonePoint;
    }


    float PointInPlaneEquation(Vector4 plane, Vector3 point)
    {
        float result = plane.x * point.x + plane.y * point.y + plane.z * point.z + plane.w;
        return result;
    }

    Vector3[] FindObjectCorner()
    {

        // Get the size of the BoxCollider
        Vector3 colliderSize = targetObjectBoxCollider.size;

        // Get the scale of the game object
        Vector3 scale = targetObject.transform.localScale;
        Quaternion rotation = targetObject.transform.rotation;
        // Scale the size by the object's scale
        Vector3 scaledSize = new Vector3(
            colliderSize.x * scale.x / 2,
            colliderSize.y * scale.y / 2,
            colliderSize.z * scale.z / 2
        );
        // Calculate the corners of the cube
        Bounds bounds = targetMeshRender.bounds;
        Vector3[] corners = new Vector3[4];
        corners[0] = bounds.center + rotation * new Vector3(scaledSize.x, scaledSize.y, scaledSize.z);
        corners[1] = bounds.center + rotation * new Vector3(scaledSize.x, scaledSize.y, -scaledSize.z);
        corners[2] = bounds.center + rotation * new Vector3(-scaledSize.x, scaledSize.y, scaledSize.z);
        corners[3] = bounds.center + rotation * new Vector3(-scaledSize.x, scaledSize.y, -scaledSize.z);

        return corners;
    }



    Vector3 FindCheckpointClosetPointOnCubeObject(Vector3 checkpointPosition)
    {

        Bounds cubeBounds = targetMeshRender.bounds;
        // Get the position of the sphere

        // Calculate the closest point on the cube to the sphere
        Vector3 closestPoint = new Vector3(
            Mathf.Clamp(checkpointPosition.x, cubeBounds.min.x, cubeBounds.max.x),
            Mathf.Clamp(checkpointPosition.y, cubeBounds.min.y, cubeBounds.max.y),
            Mathf.Clamp(checkpointPosition.z, cubeBounds.min.z, cubeBounds.max.z)
        );
        return closestPoint;
    }

    Vector3 FindCheckpointClosestPointOnCubeObject(Vector3 checkpointPosition)
    {
        return targetObjectBoxCollider.ClosestPoint(checkpointPosition);
        Bounds cubeBounds = targetMeshRender.bounds;

        //// Apply the inverse rotation to the sphere position to align it with the cube's local space
        //Vector3 alignedSpherePosition = Quaternion.Inverse(targetRotation) * (checkpointPosition - cubeBounds.center);

        //// Calculate the closest point on the cube to the aligned sphere position
        //Vector3 closestPoint = new Vector3(
        //    Mathf.Clamp(alignedSpherePosition.x, -cubeBounds.extents.x / 2, cubeBounds.extents.x / 2),
        //    Mathf.Clamp(alignedSpherePosition.y, -cubeBounds.extents.y / 2, cubeBounds.extents.y / 2),
        //    Mathf.Clamp(alignedSpherePosition.z, -cubeBounds.extents.z / 2, cubeBounds.extents.z / 2)
        //);
        //// Transform the closest point back to world space using the rotation and cube's center
        //closestPoint = targetRotation * closestPoint + cubeBounds.center;
        //return closestPoint;
    }

    public Vector4 FindPlaneEquation(Vector3 pointOnPlane, Vector3 normalVector)
    {
        float a; float b; float c; float d;
        a = normalVector.x;
        b = normalVector.y;
        c = normalVector.z;
        d = -Vector3.Dot(normalVector, pointOnPlane);
        return new Vector4(a, b, c, d);
    }
}
