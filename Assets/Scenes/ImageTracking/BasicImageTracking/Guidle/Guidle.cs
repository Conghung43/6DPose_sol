using System;
using System.Collections.Generic;
using UnityEngine;


    public class Guidle : MonoBehaviour
    {
        public Vector3[] points;
        public bool isQuadratic;
        public bool isRay;
        public Transform _start;
        public List<Transform> _endList;
        private float _time = 0.5f;

        private void Update()
        {
            
            SetLinePoint();
            //_time = 0.5f;
              
            if (StationStageIndex.stageIndex >0)
                points[3] = _endList[StationStageIndex.stageIndex - 1].transform.position;
        }

        private void SetLinePoint()
        {
            points[0] = _start.transform.position;
            float ds = Vector3.Distance(points[0], points[3]) / 2f < 0.5f
                ? Vector3.Distance(points[0], points[3]) / 2f
                : 0.5f;
            points[1] = CalculatePointOnBisector(points[0], points[3], ds);
        }

        public bool IsStartEnable()
        {
            return _start.gameObject.activeInHierarchy;
        }

        public void SetStartPoint(Transform _transform)
        {
            _start = _transform;
            SetLinePoint();
        }
        

        public Vector3 GetPoint (float t) {
            if (isQuadratic)
            {
                return transform.TransformPoint(BezierMath.GetQuadratic(points[0], points[1], points[2],points[3], t));
            }

            if (isRay)
            {
                return transform.TransformPoint(BezierMath.GetRay(points[0], points[3], t));
            }

            return transform.TransformPoint(BezierMath.GetCubic(points[0], points[1], points[3], t));
        }
        public void SetRay()
        {
            isQuadratic = false;
            isRay = true;
        }

        public void SetCurve(Vector3 pos)
        {
            isQuadratic = false;
            isRay = false;
            points[1] = pos;
        }
        Vector3 CalculatePointOnBisector(Vector3 v1, Vector3 v2, float distance)
        {
            Vector3 midpoint = new Vector3((v1.x + v2.x) / 2, (v1.y + v2.y) / 2, (v1.z + v2.z) / 2);
        
            Vector3 direction =Vector3.up; //Vector3.Cross(v2 - v1, Vector3.up).normalized;
        
            Vector3 pointOnBisector = midpoint + direction * distance;
        
            return pointOnBisector;
        }

        public void SetCurveCubic(Vector3 p1,Vector3 p2)
        {
            isQuadratic = true;
            isRay = false;
        }
    }








    public static class BezierMath
    {
        public static Vector3 GetQuadratic (Vector3 p0, Vector3 p1, Vector3 p2,Vector3 p3, float t) {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * oneMinusT * p0 +
                3f * oneMinusT * oneMinusT * t * p1 +
                3f * oneMinusT * t * t * p2 +
                t * t * t * p3;
        }
        public static Vector3 GetCubic (Vector3 p0, Vector3 p1, Vector3 p3, float t) {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return
                oneMinusT * oneMinusT * p0 +
                2f * oneMinusT * t * p1 +
                t * t * p3;
        }

        public static Vector3 GetRay(Vector3 p0, Vector3 p3, float t)
        {
            return Vector3.Lerp(p0,p3 , t);
        }
    }
