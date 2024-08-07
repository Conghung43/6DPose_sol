using System.Collections.Generic;
using UnityEngine;


    public class SideStep : MonoBehaviour
    {
        public Guidle curve;
        public GameObject targetObject;
        public BoxCollider targetObjectBoxCollider;
        public MeshRenderer targetMeshRender;
        public Transform checkpointPosition;
        Vector3 startPoint;
        private Vector3[] topCorners;
        private List<Vector3> mileStoneClasify;
        public float ratio,maxDistance,minDistance;

        private void Start()
        {
            
        }

        private void Update()
        {
            topCorners = FindObjectCorner();
            startPoint = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 4, 0.3f));
            Vector3 checkpointReflectOnCube = FindReflectCheckpointOnCubeObject(checkpointPosition.position);
            Vector4 planeContainCheckpointClosetPoint = FindPlaneEquation(checkpointReflectOnCube, checkpointPosition.position - checkpointReflectOnCube);
            Vector3[] cameraPosition = { Camera.main.transform.position };
            mileStoneClasify = ClasifyPointsBasedPlane(planeContainCheckpointClosetPoint, cameraPosition, targetObject.transform.position);
            if (mileStoneClasify.Count == 1)
            {
                curve.SetRay();
            }
            else
            {
                Vector4 CameraCheckpointVerticalPlane = FindVerticalPlane(checkpointPosition.position, Camera.main.transform.position);

                Vector3 horizontalPlaneNormalVector = Vector3.Cross(CameraCheckpointVerticalPlane, startPoint - checkpointPosition.position);

                Vector4 CameraCheckpointHorizontalPlane = FindPlaneEquation(startPoint, horizontalPlaneNormalVector);

                // Find cut position of plane with box corner
                Vector3[] cutCornerPoints = FindCutCornerPoints(CameraCheckpointHorizontalPlane);

                // clasify points
                mileStoneClasify = ClasifyPointsBasedPlane(CameraCheckpointVerticalPlane, cutCornerPoints, targetObject.transform.position);
                float distanceCamera = Vector3.Distance(cameraPosition[0], targetObject.transform.position)*ratio;
                if (distanceCamera > maxDistance)
                {
                    distanceCamera = maxDistance;
                }
                if (distanceCamera < minDistance)
                {
                    distanceCamera = minDistance;
                }
                if (mileStoneClasify.Count==1)
                {
                    curve.SetCurve(mileStoneClasify[0]-(targetObject.transform.position-mileStoneClasify[0])*distanceCamera);
                }
                if(mileStoneClasify.Count==2)
                {
                    Vector3 p1 = mileStoneClasify[0] -
                                 (targetObject.transform.position - mileStoneClasify[0]).normalized * distanceCamera;
                    Vector3 p2 = mileStoneClasify[1] -
                                 (targetObject.transform.position - mileStoneClasify[1]).normalized * distanceCamera;
                    if (Vector3.Distance(cameraPosition[0], p1) > Vector3.Distance(cameraPosition[0], p2))
                    {
                        (p1, p2) = (p2, p1);
                    }
                    curve.SetCurveCubic(
                        
                        p1,
                        p2);
                }
                
                /*for (int i = 0; i < mileStoneClasify.Count; i++)
                {
                    Debug.Log(mileStoneClasify[i]);
                }*/
            }
            
        }

        private void OnDrawGizmos()
        {
            Gizmos.color=Color.blue;
            if (mileStoneClasify != null)
            {
                for (int i = 0; i < mileStoneClasify.Count; i++)
                {
                    Gizmos.DrawSphere(mileStoneClasify[i],0.05f);
                }
                
            }
        }

        Vector4 FindVerticalPlane(Vector3 point1, Vector3 point2)
        {
            // Calculate the direction vector of the line AB
            Vector3 direction = point1 - point2;

            // perpendicular vector in the x-z plane
            Vector3 perpendicular = new Vector3(0, 1f, 0).normalized;

            perpendicular = Vector3.Cross(direction, perpendicular);
            return new Vector4(perpendicular.x, perpendicular.y, perpendicular.z, -Vector3.Dot(perpendicular, point1));
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
        Vector3 FindReflectCheckpointOnCubeObject(Vector3 checkpointPosition)
        {
            return targetObjectBoxCollider.ClosestPoint(checkpointPosition);
            
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
        List<Vector3> ClasifyPointsBasedPlane(Vector4 plane, Vector3[] points, Vector3 targetPosition)
        {
            bool pointPlaneRelationship = PointOnPlane(plane, targetPosition) > 0;
            //Debug.Log(centerSide + verticalPlane.ToString());
            List<Vector3> mileStonePoint = new List<Vector3>();
            for (int i = 0; i < points.Length; i++)
            {
                bool pointSide = PointOnPlane(plane, points[i]) > 0;
                if (pointSide != pointPlaneRelationship)
                {
                    mileStonePoint.Add(points[i]);
                }
            }
            return mileStonePoint;
        }
        float PointOnPlane(Vector4 plane, Vector3 point)
        {
            float result = plane.x * point.x + plane.y * point.y + plane.z * point.z + plane.w;
            return result;
        }
        Vector3[] FindCutCornerPoints(Vector4 Plane)
        {
            Vector3[] corners = new Vector3[4];
            for (int i = 0; i < corners.Length; i++)
            {
                float yValue = (-Plane.w - topCorners[i].x * Plane.x - topCorners[i].z * Plane.z) / Plane.y;
                corners[i] = new Vector3(topCorners[i].x, yValue, topCorners[i].z);
            }
            return corners;
        }
    }

