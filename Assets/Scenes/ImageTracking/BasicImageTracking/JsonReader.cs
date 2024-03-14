using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using UnityEngine.Networking;
using System.Net;
using System.Net.NetworkInformation;
using static UnityEngine.XR.ARFoundation.Samples.DynamicLibrary;
using UnityEngine.XR.ARFoundation.Samples;

namespace UnityEngine.XR.ARFoundation.Samples
{

    [System.Serializable]
    public class InferenceResult
    {
        public Data data;
        public string message;
        public bool result;
    }

    [System.Serializable]
    public class Data
    {
        public float[] obj_pose;
    }

    public class JsonReader : MonoBehaviour
    {
        // Path to the JSON file
        //string jsonFilePath = "/Users/hungnguyencong/Downloads/out/poses.json";
        string txtFilePath = "/Users/hungnguyencong/Downloads/camera_poses_pred3.txt";
        string txtFilePath1 = "/Users/hungnguyencong/Downloads/pose_1_476.txt";
        public GameObject originTransform;
        public GameObject arPose;
        public GameObject megaPose;
        public GameObject box3D;
        //[SerializeField] public TMPro.TextMeshProUGUI logInfo;
        //float[] positions = new float[111];
        //float[] positions1 = new float[111];
        void Start()
        {
            //Camera.main.transform.position = Vector3.zero;

            //ReadTXTFromFile(txtFilePath);
            //ReadTXTFromFile(txtFilePath1);
        }

        public static IEnumerator ServerInference(byte[] imageData, int[] tlrbBox)
        {
            string url = $"http://10.1.2.148:6996/inference";
            //this.ResizeImage(imagePath);
            //byte[] imageData = File.ReadAllBytes(imagePath);
            //File.WriteAllBytes($"{tlrbBox[0]}_{tlrbBox[1]}_{tlrbBox[2]}_{tlrbBox[3]}.jpg", imageData);
            //imageData = System.IO.File.ReadAllBytes("/Users/hungnguyencong/Documents/PYTHON/API_Test_Python/test/1_3.jpg");
            //tlrbBox = new int[] { 458, 70, 1340, 950 };

            string filePath = Path.Combine(Application.persistentDataPath, "test.jpg");
            //System.IO.File.WriteAllBytes(filePath, imageData);
            //File.WriteAllBytes("test.jpg", imageData);

            WWWForm form = new WWWForm();
            form.AddBinaryData("img", imageData, "image.jpg", "image/jpeg");

            string bboxData = "{\"bboxes\": [";
            for (int i = 0; i < tlrbBox.Length; i += 4)
            {
                bboxData += "[" + tlrbBox[i] + "," + tlrbBox[i + 1] + "," + tlrbBox[i + 2] + "," + tlrbBox[i + 3] + "]";
                if (i < tlrbBox.Length - 4)
                {
                    bboxData += ",";
                }
            }
            bboxData += "]}";
            form.AddField("data", bboxData);

            string jsonBox = "[" + string.Join(",", tlrbBox) + "]";
            form.AddField("data", jsonBox);
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            {
                //request.SetRequestHeader("bboxes", jsonBox);
                request.certificateHandler = new CertificateVS();
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error: " + request.error);
                }
                else
                {
                    InferenceResult result = JsonUtility.FromJson<InferenceResult>(request.downloadHandler.text);
                    JsonReader.Set3DBox(result.data.obj_pose);
                }
            }
        }

        public static (Quaternion, Vector3) ConvertTransformToLeftHand(Quaternion rotation, Vector3 position)
        {

            // Convert rotation from right-hand rule (pose B) to left-hand rule (pose A)
            rotation = new Quaternion(-rotation.x, rotation.y, rotation.z, -rotation.w);
            // Convert position from right-hand rule (pose B) to left-hand rule (pose A)
            position = new Vector3(-position.x, position.y, position.z);
            return (rotation, position);
        }

        public static (Quaternion, Vector3) InverseTransformation(Quaternion quaternion, Vector3 translation)
        {
            // Convert Quaternion and Translation to Matrix4x4
            Matrix4x4 matrix = QuaternionTranslationToMatrix(quaternion, translation);

            // Invert the matrix
            Matrix4x4 invertedMatrix = matrix.inverse;

            // Convert inverted Matrix4x4 to Quaternion and Translation
            MatrixToQuaternionTranslation(invertedMatrix, out Quaternion invertedQuaternion, out Vector3 invertedTranslation);

            return (invertedQuaternion, invertedTranslation);
        }

        public static Matrix4x4 QuaternionTranslationToMatrix(Quaternion quaternion, Vector3 translation)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(translation, quaternion, Vector3.one);
            return matrix;
        }

        // Convert Matrix4x4 to Quaternion and Translation
        public static void MatrixToQuaternionTranslation(Matrix4x4 matrix, out Quaternion quaternion, out Vector3 translation)
        {
            quaternion = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
            translation = matrix.GetColumn(3);
        }

        public static void Set3DBox(float[] objectPose)
        {
            Quaternion rotation = new Quaternion(objectPose[1], objectPose[2], objectPose[3], objectPose[0]);
            Vector3 position = new Vector3(objectPose[4], objectPose[5], objectPose[6]);
            //DecomposeMatrix(matrix, out rotation, out position);



            (rotation, position) = ConvertTransformToLeftHand(rotation, position);

            //GameObject megaCam = GameObject.Find("MegaCam");
            //megaCam.transform.position = position;
            //megaCam.transform.rotation = rotation;

            //Matrix4x4 matrixCamInWorld = Camera.main.transform.localToWorldMatrix;
            //Matrix4x4 matrixObjInCam = QuaternionTranslationToMatrix(rotation, position);

            //Matrix4x4 matrixObjInWorld = matrixCamInWorld * matrixObjInCam.inverse;
            //MatrixToQuaternionTranslation(matrixObjInWorld, out rotation, out position);

            //Matrix4x4 objectMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            // Get the camera's pose matrix
            Matrix4x4 cameraMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);
            // Transform the object from object space to camera space
            //Matrix4x4 objectInCameraMatrix = cameraMatrix * objectMatrix.inverse;
            //Matrix4x4 matrixCamInWorld = Camera.main.cameraToWorldMatrix;
            Matrix4x4 matrixCamInWorld1 = Camera.main.transform.localToWorldMatrix;
            Matrix4x4 matrixObjInWorld = matrixCamInWorld1 * cameraMatrix.inverse;
            MatrixToQuaternionTranslation(matrixObjInWorld, out rotation, out position);
            Display3DBox(position);

            //GameObject megaObject = new GameObject("NewObject");
            //megaObject.transform.position = Vector3.zero;

            //PoseConversion(megaCam.transform, Camera.main.transform, Vector3.zero);
        }

        public static void Display3DBox(Vector3 position)
        {
            // sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject obj = GameObject.Find("cube_frame");
            obj.transform.position = position;
            Debug.Log(position.ToString());
            TrackedImageInfoManager.drawObject = true;
        }

        string[] RemoveFirstElementFromArray(string[] array)
        {
            // Check if the array is not empty
            if (array.Length > 0)
            {
                // Create a new array with length - 1
                string[] newArray = new string[array.Length - 1];

                // Copy elements from index 1 to the end of the original array to the new array
                System.Array.Copy(array, 1, newArray, 0, newArray.Length);

                // Return the new array
                return newArray;
            }
            else
            {
                // If the array is empty, return the original array
                return array;
            }
        }

        void ReadTXTFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                //float[] positions = new float[59];
                bool unityGenerate = false;
                Vector3 objectScale = new Vector3(0.4f, 0.4f, 0.4f);
                var jsonContent = File.ReadAllLines(filePath);
                for (int i = 0; i < jsonContent.Length; i++)
                {
                    string poseString = jsonContent[i];
                    var stringSplit = poseString.Split(' ');
                    if (stringSplit[0].Contains("jpg"))
                    {
                        stringSplit = RemoveFirstElementFromArray(stringSplit);
                        unityGenerate = true;
                    }
                    //matrix = RightHandToLeftHand(matrix);
                    Quaternion rotation = new Quaternion(float.Parse(stringSplit[1]),
                                                            float.Parse(stringSplit[2]),
                                                            float.Parse(stringSplit[3]),
                                                            float.Parse(stringSplit[0])
                                                            );
                    Vector3 position = new Vector3(float.Parse(stringSplit[4]),
                                                            float.Parse(stringSplit[5]),
                                                            float.Parse(stringSplit[6])
                                                            ); ;
                    //DecomposeMatrix(matrix, out rotation, out position);
                    if (!unityGenerate)
                    {
                        (rotation, position) = ConvertTransformToLeftHand(rotation, position);

                        (rotation, position) = InverseTransformation(rotation, position);


                        Matrix4x4 matrixCamInWorld = Camera.main.transform.localToWorldMatrix;
                        Matrix4x4 matrixObjInCam = QuaternionTranslationToMatrix(rotation, position);

                        Matrix4x4 matrixObjInWorld = matrixCamInWorld * matrixObjInCam;
                        MatrixToQuaternionTranslation(matrixObjInWorld, out rotation, out position);

                        // PoseConversion(megaCam.transform, Camera.main.transform, Vector3.zero);


                        GameObject nodeGameObject = Instantiate(originTransform, position, rotation);
                        nodeGameObject.transform.SetParent(megaPose.transform);

                    }
                    //position[0] = -position[0];
                    else
                    {
                        GameObject nodeGameObject = Instantiate(originTransform, position, rotation);
                        nodeGameObject.transform.SetParent(arPose.transform);
                    }
                    //nodeGameObject.transform.localScale = objectScale;
                    //if (!unityGenerate)
                    //{
                    //    positions[i] = Vector3.Distance(Vector3.zero, position);
                    //}
                    //else
                    //{
                    //    positions1[i] = Vector3.Distance(Vector3.zero, position);
                    //}
                    if (i >= 100)
                    {
                        //Debug.Log(positions.ToString());
                        break;
                    }
                }
            }
            else
            {
                Debug.LogError("JSON file not found at path: " + filePath);
            }
        }

        void ReadJSONFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                ParseJSON(jsonContent);
            }
            else
            {
                Debug.LogError("JSON file not found at path: " + filePath);
            }
        }

        void ParseJSON(string jsonContent)
        {
            // Parse the JSON content into a Dictionary
            var imageData = JsonConvert.DeserializeObject<Dictionary<string, List<List<float>>>>(jsonContent);
            // Accessing the image data
            foreach (KeyValuePair<string, List<List<float>>> pair in imageData)
            {
                Matrix4x4 matrix = ConvertListToMatrix(pair.Value);
                //matrix = RightHandToLeftHand(matrix);
                Quaternion rotation;
                Vector3 position;
                DecomposeMatrix(matrix, out rotation, out position);
                (rotation, position) = ConvertTransformToLeftHand(rotation, position);
                position[0] = -position[0];
                GameObject nodeGameObject = Instantiate(originTransform, position, rotation);
                nodeGameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            }
        }

        // Function to convert a List<List<float>> to a Matrix4x4
        public static Matrix4x4 ConvertListToMatrix(List<List<float>> list)
        {
            Matrix4x4 matrix = Matrix4x4.identity;

            // Assign values to the matrix
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    matrix[i, j] = list[i][j];
                }
            }

            return matrix;
        }

        // Function to convert a 4x4 affine transformation matrix to rotation and position
        public static void ConvertMatrixToRotationAndPosition(Matrix4x4 matrix, out Quaternion rotation, out Vector3 position)
        {
            // Extract rotation
            rotation = matrix.rotation;

            // Extract translation (position)
            position = matrix.GetColumn(3);
        }

        private void DecomposeMatrix(Matrix4x4 matrix, out Quaternion rotation, out Vector3 position)
        {
            // Extract position from the matrix
            position = matrix.GetColumn(3);

            // Extract rotation from the matrix
            Vector3 scale;
            scale.x = matrix.GetColumn(0).magnitude;
            scale.y = matrix.GetColumn(1).magnitude;
            scale.z = matrix.GetColumn(2).magnitude;

            // Normalize the matrix
            Matrix4x4 normalizedMatrix = matrix;
            normalizedMatrix.SetColumn(0, matrix.GetColumn(0) / scale.x);
            normalizedMatrix.SetColumn(1, matrix.GetColumn(1) / scale.y);
            normalizedMatrix.SetColumn(2, matrix.GetColumn(2) / scale.z);

            rotation = Quaternion.LookRotation(normalizedMatrix.GetColumn(2), normalizedMatrix.GetColumn(1));
        }

        public class CertificateVS : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }
    }
}

//objectScale = new Vector3(0.2f, 0.2f, 0.2f);
//Vector3 eula = rotation.eulerAngles;
//eula.x = eula.x + 90;
//eula.y = eula.y + 180;
//Quaternion rotationZaxis = Quaternion.Euler(0, 180, 0);
//position = rotationZaxis * position;
//position.y = position.y + 0.15f;
//rotation = Quaternion.Euler(eula);
//rotation = RotateQuaternionAroundX(rotation, -90);

// Function to convert a point from one system to another
//public static void ConvertPoint(Transform from, Transform to, Vector3 point)
//{
//    Vector3 positionOffset = to.position - from.position;
//    Quaternion rotationOffset = Quaternion.Inverse(from.rotation) * to.rotation;

//    // Construct the transformation matrix
//    Matrix4x4 matrix = Matrix4x4.TRS(positionOffset, rotationOffset, Vector3.one);

//    Vector3 pointInSystemB = matrix.MultiplyPoint3x4(point);

//    GameObject obj = GameObject.Find("cube_frame");
//    obj.transform.position = pointInSystemB;
//    TrackedImageInfoManager.drawObject = true;
//}

//public static void PoseConversion1(Transform objectTransformInSystemA, Transform objectTransformInSystemB, Vector3 objectTransformInSourcePosition)
//{
//    Matrix4x4 matrixA = objectTransformInSystemA.localToWorldMatrix;
//    Matrix4x4 matrixB = objectTransformInSystemB.localToWorldMatrix;

//    // Inverse of matrixA
//    Matrix4x4 inverseMatrixA = matrixA.inverse;

//    // Transformation matrix from system A to system B
//    Matrix4x4 transformationMatrix = matrixB * inverseMatrixA;

//    Vector3 pointInSystemB = transformationMatrix.MultiplyPoint3x4(objectTransformInSourcePosition);

//    GameObject obj = GameObject.Find("cube_frame");
//    obj.transform.position = pointInSystemB;
//    TrackedImageInfoManager.drawObject = true;

//}