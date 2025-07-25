using UnityEngine;

public class FaceToUser : MonoBehaviour
{
    public float XPosition, YPosition, ZPosition = 800;

    void Update()
    {
        transform.position = Camera.main.transform.position - Vector3.forward * 800 +
                             Camera.main.transform.forward * ZPosition +
                             Camera.main.transform.right * XPosition +
                             Camera.main.transform.up * YPosition;

        transform.rotation = Camera.main.transform.rotation;
    }
}