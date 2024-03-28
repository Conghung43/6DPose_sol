using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    private Transform[] transforms = new Transform[5];
    // Dictionary to store groups of transforms
    




    void Start()
    {
        // Check if there are transforms to group
        if (transforms == null || transforms.Length == 0)
        {
            Debug.LogWarning("No transforms to group.");
            return;
        }
        //UpdateTransformToGroup(null);
    }


}