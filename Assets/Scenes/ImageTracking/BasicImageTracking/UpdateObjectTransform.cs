using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class UpdateObjectTransform : MonoBehaviour
{

    public static Dictionary<Transform, List<Transform>> groupedTransforms = new Dictionary<Transform, List<Transform>>();

    public static float positionThreshold = 0.05f; // Adjust as needed
    public static float rotationThreshold = 2f;    // Adjust as needed

    public static Transform UpdateTransformToGroup(Transform currentTransform)
    {
        // Find a group for the current transform
        bool foundGroup = false;
        foreach (KeyValuePair<Transform, List<Transform>> pair in groupedTransforms)
        {
            Transform groupCenter = pair.Key;
            List<Transform> group = pair.Value;

            // Check if the current transform is similar to the group center
            if (IsSimilar(currentTransform, groupCenter))
            {
                group.Add(currentTransform);
                foundGroup = true;
                if (group.Count > 5)
                {
                    //update
                    Transform averageTransform = GetTransformAverage(group);
                    // Reset group
                    DestroyAllGameObject();
                    groupedTransforms.Clear();
                    return averageTransform;
                }
                else
                {
                    return null;
                }
            }
        }
        // Limit lenth of dictionary
        if (groupedTransforms.Count > 5)
        {
            DestroyAllGameObject();
            groupedTransforms.Clear();
        }
        else
        {
            // If no similar group is found, create a new group
            if (!foundGroup)
            {
                List<Transform> newGroup = new List<Transform>();
                newGroup.Add(currentTransform);
                groupedTransforms.Add(currentTransform, newGroup);
            }
        }
        return null;
    }

    public static void DestroyAllGameObject()
    {
        foreach (KeyValuePair<Transform, List<Transform>> pair in groupedTransforms)
        {
            List<Transform> group = pair.Value;
            foreach (Transform t in group)
            {
                Destroy(t.gameObject);
            }
        }
    }

    // Function to check if two transforms are similar based on position and rotation
    public static bool IsSimilar(Transform t1, Transform t2)
    {
        float positionDifference = Vector3.Distance(t1.position, t2.position);
        float rotationDifference = Quaternion.Angle(t1.rotation, t2.rotation);

        return (positionDifference <= positionThreshold && rotationDifference <= rotationThreshold);
    }

    public static Transform GetTransformAverage(List<Transform> transforms)
    {
        // Initialize variables to hold the sum of positions, rotations, and scales
        Vector3 sumPosition = Vector3.zero;
        Quaternion sumRotation = Quaternion.identity;
        Vector3 sumScale = Vector3.zero;

        // Iterate through each transform and accumulate the sums
        foreach (Transform t in transforms)
        {
            sumPosition += t.position;
            sumRotation *= t.rotation;
            sumScale += t.localScale;
        }

        // Calculate the average position, rotation, and scale
        Vector3 averagePosition = sumPosition / transforms.Count;
        Quaternion averageRotation = Quaternion.Euler(sumRotation.eulerAngles / transforms.Count);
        Vector3 averageScale = sumScale / transforms.Count;

        // Create a new GameObject to represent the average transform
        GameObject averageObject = GameObject.Find("AverageTransform");
        if (averageObject == null)
        {
            averageObject = new GameObject("AverageTransform");
        }
        Transform averageTransform = averageObject.transform;
        averageTransform.position = averagePosition;
        averageTransform.rotation = averageRotation;
        averageTransform.localScale = averageScale;
        averageObject.AddComponent<ARAnchor>();

        return averageTransform;
    }
    public static Vector2[] GetPoints2D(GameObject cubeGameObject)
    {
        Vector3 cubePosition = cubeGameObject.transform.position;
        Quaternion cubeRotation = cubeGameObject.transform.rotation;
        Vector3 cubeScale = cubeGameObject.transform.localScale;
        Vector3[] cornerOffsets = new Vector3[]
            {
                new Vector3(-1f, -1f, -1f),
                new Vector3( 1f, -1f, -1f),
                new Vector3(-1f,  1f, -1f),
                new Vector3( 1f,  1f, -1f),
                new Vector3(-1f, -1f,  1f),
                new Vector3( 1f, -1f,  1f),
                new Vector3(-1f,  1f,  1f),
                new Vector3( 1f,  1f,  1f)
            };

        Vector2[] bbox = new Vector2[8];

        for (int i = 0; i < cornerOffsets.Length; i++)
        {
            Vector3 position = cubePosition + cubeRotation * Vector3.Scale(cubeScale * 0.5f, cornerOffsets[i]);
            Vector2 EdgePoint = Camera.main.WorldToScreenPoint(position) ;
            bbox[i] = new Vector2(EdgePoint.x, Screen.height - EdgePoint.y);
        }
        return bbox;
    }
}
