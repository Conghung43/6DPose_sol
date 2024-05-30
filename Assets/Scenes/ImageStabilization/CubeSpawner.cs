using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using System;
using Unity.Collections.LowLevel.Unsafe;
using System.Net.NetworkInformation;
using Unity.VisualScripting;
using System.Collections;

public class CubeSpawner : MonoBehaviour
{
    public GameObject cubePrefab;
    public GameObject parent;
    private int numberOfCubes = 1000;
    private float cubeWidth = 0.0194f;
    private float cubeDepth = 0.0136f;
    private float areaWidth = 0.640f;  // Define the width of the spawning area
    private float areaDepth = 0.480f;  // Define the depth of the spawning area
    public Button startButton;
    public RenderTexture m_renderTexture;
    public Texture2D m_screenShot;
    int maxAttempts = 0;

    void Start()
    {
        m_renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        m_screenShot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        StartCoroutine(SpawnCubes());
    }

    IEnumerator SpawnCubes()
    {
        List<Bounds> occupiedBounds;
        for (int maxAttempts = 100; maxAttempts < 10001; maxAttempts+=100)
        {
            foreach (Transform child in parent.transform)
            {
                Destroy(child.gameObject);
            }
            startButton.gameObject.SetActive(false);

            yield return new WaitForSeconds(1f);

            //int maxAttempts = 1;//numberOfCubes *15;
            int cubesSpawned = 0;
            int attempts = 0;
            occupiedBounds = null; 
            occupiedBounds = new List<Bounds>();

            // Create or overwrite the text file
            //string filePath = Path.Combine(Application.persistentDataPath, $"{attempts}_label.txt");
            StreamWriter writer = new StreamWriter($"Images/{maxAttempts}_label.txt", false);
            //UpdateCPUImage(attempts);

            while (cubesSpawned < numberOfCubes && attempts < maxAttempts)
            {
                Vector3 randomPosition = new Vector3(
                    (float)UnityEngine.Random.Range(-areaWidth / 2, areaWidth / 2),
                    0.125f / 2,  // Half the height of the cube to place it on the OXZ plane
                    (float)UnityEngine.Random.Range(-areaDepth / 2, areaDepth / 2)
                );
                for (int index = 0; index < 180; index += 10)
                {
                    Quaternion randomRotation = Quaternion.Euler(0, index, 0);

                    if (!IsOverlapping(randomPosition, randomRotation, occupiedBounds))
                    {
                        GameObject cube = Instantiate(cubePrefab, randomPosition, randomRotation);
                        occupiedBounds.Add(cube.GetComponent<Renderer>().bounds);
                        Vector2[] corners = GetPoints2D(cube.transform);
                        string text = "0 ";
                        for (int i = 0; i <  corners.Length; i++)
                        {
                            Vector2 corner = corners[i];
                            text += Math.Round(corner.x/Screen.width,3).ToString() + " " + Math.Round((Screen.height-corner.y)/Screen.height, 3).ToString() ;
                            if (i < corners.Length - 1)
                            {
                                text += " ";
                            }
                        }
                        text += "\n";
                        writer.WriteLine(text);
                        cubesSpawned++;
                        cube.transform.SetParent(parent.transform);
                        break;

                    }
                }
                attempts++;
            }

            writer.Close();

            yield return new WaitForSeconds(1f);

            SendImage2Meta(maxAttempts);

        

        occupiedBounds.Clear();
            //break;

            //if (cubesSpawned < numberOfCubes)
            //{
            //    Debug.LogWarning("Could not spawn all cubes within the max attempts.");
            //}
        }

    }



    bool IsOverlapping(Vector3 position, Quaternion rotation, List<Bounds> occupiedBounds)
    {
        GameObject tempCube = Instantiate(cubePrefab, position, rotation);
        Bounds tempBounds = tempCube.GetComponent<Renderer>().bounds;
        Destroy(tempCube);

        foreach (Bounds occupied in occupiedBounds)
        {
            if (tempBounds.Intersects(occupied))
            {
                return true;
            }
        }
        return false;
    }

    Vector2[] GetPoints2D(Transform cubeGameObject)
    {
        Vector3 cubePosition = cubeGameObject.position;
        Quaternion cubeRotation = cubeGameObject.rotation;
        Vector3 cubeScale = cubeGameObject.localScale;
        Vector2[] corner = new Vector2[4];
        Vector3[] cornerOffsets = new Vector3[]
            {
                //new Vector3(-1f, -1f, -1f),
                //new Vector3( 1f, -1f, -1f),
                new Vector3(-1f,  1f, -1f),
                new Vector3( 1f,  1f, -1f),
                //new Vector3(-1f, -1f,  1f),
                //new Vector3( 1f, -1f,  1f),
                new Vector3( 1f,  1f,  1f),
                new Vector3(-1f,  1f,  1f)
                
            };

        for (int i = 0; i < cornerOffsets.Length; i++)
        {
            Vector3 position = cubePosition + cubeRotation * Vector3.Scale(cubeScale * 0.5f, cornerOffsets[i]);
            corner[i] = Camera.main.WorldToScreenPoint(position);
        }
        return corner;
    }

    // Start is called before the first frame update
    void CreateSphere(Vector3 position)
    {
        // Create a sphere GameObject
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        // Set the position of the sphere
        sphere.transform.position = position;

        // Set the scale of the sphere
        sphere.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
    }


    public ARCameraManager m_CameraManager;
    private Texture2D m_CameraTexture;


    private void SendImage2Meta(int maxAttempts)
    {
        // Set the target texture of the AR camera to the render texture
        Camera.main.targetTexture = m_renderTexture;

        // Render the AR camera
        Camera.main.Render();

        // Set the active render texture
        RenderTexture.active = m_renderTexture;

        // Read the pixels from the specified rectangle in the capture texture
        m_screenShot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);

        // Apply the changes made to the capture texture
        m_screenShot.Apply();

        // Encode the capture texture as JPG and assign it to the CapturedImage variable
        byte[] CapturedImage = m_screenShot.EncodeToJPG();

        File.WriteAllBytes($"Images/{maxAttempts}_label.jpg", CapturedImage);

        // Reset the target and active render textures
        Camera.main.targetTexture = null;
        RenderTexture.active = null;
    }

}