using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using UnityEngine.Networking;
using System.Net;
using System.Net.NetworkInformation;
using UnityEngine.XR.ARFoundation.Samples;
using UnityEngine.UIElements;
using TMPro;

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

    public class Inference : MonoBehaviour
    {
        // Path to the JSON file
        //string jsonFilePath = "/Users/hungnguyencong/Downloads/out/poses.json";
        //string InferenceTxtFilePath = "/Users/hungnguyencong/Documents/PYTHON/API_Test_Python/InferencePose325_1.txt";
        //string ARTxtFilePath = "/Users/hungnguyencong/Downloads/pose_1_123.txt";
        public GameObject originTransform;
        public GameObject boxOnWorldObject;
        public GameObject camOnObject;
        public GameObject arCam;
        public GameObject megaPose;
        public GameObject box3D;
        private Dictionary<string, GameObject> CamWorldDictionary = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> CamObjectDictionary = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> CamObjectMegaDictionary = new Dictionary<string, GameObject>();
        public static Matrix4x4 CameraMatrix = new Matrix4x4();
        public static bool objectInitialSet = true;
        public static float[] arPoseToInference = null;

        //[SerializeField] private static TMPro.TextMeshProUGUI logInfo;
        //float[] positions = new float[111];
        //float[] positions1 = new float[111];
        void Start()
        {

            //box3D.transform.position = new Vector3(-0.004634091f, - 0.1383801f, 2.226088f);////0.08501446f, - 0.1245978f, 2.024555f
            //box3D.transform.rotation = new Quaternion(0, 0, 0, 1);
            //ReadTXTFromFile(ARTxtFilePath);
            //ReadTXTFromFile(InferenceTxtFilePath);
        }

        private void OnDisable()
        {
            arPoseToInference = null;
            objectInitialSet = true;
        }


        public static IEnumerator ServerInference(byte[] imageData, Vector2 imageSize, int[] tlrbBox, Vector2 focalLength, Vector2 principalPoint)
        {
            string url = $"https://10.1.2.148:5000/sol_server/inference/6dpose";

            // Get current camera matrix:
            CameraMatrix = Camera.main.transform.localToWorldMatrix;

            //string filePath = Path.Combine(Application.persistentDataPath, $"{tlrbBox[0]}_{tlrbBox[1]}_{tlrbBox[2]}_{tlrbBox[3]}.jpg");
            //System.IO.File.WriteAllBytes(filePath, imageData);
            //File.WriteAllBytes("test.jpg", imageData);
            //Debug.Log(tlrbBox.ToString());
            if (!objectInitialSet)
            {
                arPoseToInference = null;
                ConvertARposeToMegaPose();
            }

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
            bboxData += "],";
#if UNITY_EDITOR
            //focalLength = new Vector2(934.098886308209f, 933.9920158878367f);
            //principalPoint = new Vector2(959.7212318150472f, 539.8662057950421f);
            // New camera intrinsic
            focalLength = new Vector2(936.2321683838078f, 936.1081714012856f);
            principalPoint = new Vector2(959.2009481268866f, 538.9017422822632f);
#endif
            if (arPoseToInference != null)
            {
                bboxData += "\"init_pose\": [";
                for (int i = 0; i < arPoseToInference.Length; i ++)
                {
                    bboxData += arPoseToInference[i].ToString();
                    if (i < arPoseToInference.Length - 1)
                    {
                        bboxData += ",";
                    }
                }
                bboxData += "],";
            }
            else
            {
                bboxData += "\"init_pose\":\"None\",";
            }
            
            bboxData += " \"project\":\"airpump\", \"camera_data\": {\"K\":[[" + focalLength.x.ToString() + ",0.0,"+ principalPoint.x.ToString() +"],[0.0,"+ focalLength.y.ToString() +"," +principalPoint.y.ToString() +"], [0.0,0.0,1.0]],\"resolution\": [" + imageSize.y.ToString() + "," + imageSize.x.ToString()+"]}}";

            form.AddField("data", bboxData);

            string jsonBox = "[" + string.Join(",", tlrbBox) + "]";
            form.AddField("data", jsonBox);
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            {
                request.certificateHandler = new CertificateVS();
                request.SetRequestHeader("Tool-Name", "6dpose");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error: " + request.error);
                }
                else
                {
                    InferenceResult result = JsonUtility.FromJson<InferenceResult>(request.downloadHandler.text);
                    if (result.data.obj_pose != null)
                    {
                        Inference.Set3DBox(result.data.obj_pose);
                    }
                    else
                    {
                        TrackedImageInfoManager.isInferenceAvailable = true;
                    }
                }
            }
        }


        public static (Quaternion, Vector3) ConvertToOppositeHandedness(Quaternion rotation, Vector3 position)
        {
            rotation = new Quaternion(rotation.x, -rotation.y, rotation.z, -rotation.w);
            position = new Vector3(position.x, -position.y, position.z);
            return (rotation, position);
        }

        //public static (Quaternion, Vector3) ConvertTransformToRightHandRule(Quaternion rotation, Vector3 position)
        //{
        //    rotation = new Quaternion(-rotation.x, rotation.y, -rotation.z, rotation.w);
        //    position = new Vector3(position.x, -position.y, position.z);
        //    return (rotation, position);
        //}

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

            (rotation, position) = ConvertToOppositeHandedness(rotation, position);

            Matrix4x4 CamToObjectMatrixMega = Matrix4x4.TRS(position, rotation, Vector3.one);//;

            Matrix4x4 ObjectToWorldMatrix = CameraMatrix * CamToObjectMatrixMega.inverse;

            MatrixToQuaternionTranslation(ObjectToWorldMatrix, out rotation, out position);

            GameObject megaPoseEstimateGameObject = new GameObject();
            megaPoseEstimateGameObject.name = "megaPoseEstimateGameObject";
            megaPoseEstimateGameObject.transform.position = position;
            megaPoseEstimateGameObject.transform.rotation = rotation;
            megaPoseEstimateGameObject.transform.localScale = Vector3.one;

            Transform updatedTransform =  UpdateObjectTransform.UpdateTransformToGroup(megaPoseEstimateGameObject.transform);
            if (updatedTransform != null)
            {
                //Display3DBox("AirPump3dBox", updatedTransform.position, updatedTransform.rotation);
                Display3DBox("ModelTarget", position, rotation);//Haven't use the average pose yetq
                objectInitialSet = false;
                StationStageIndex.ModelTargetFound = true;
            }
            //Display3DBox("AirPump3DModel", position, rotation);
            if (objectInitialSet)
            {
                Display3DBox("ModelTarget", position, rotation);
                //StationStageIndex.ModelTargetFound = true;
            }
            Display3DBox("AirPump3DModel", position, rotation);

            TrackedImageInfoManager.isInferenceAvailable = true;
        }

        public static void ConvertARposeToMegaPose()
        {
            GameObject filterObj = GameObject.Find("AirPump3dBox");
            if (filterObj != null)//(objectInitialSet)
            {
                Matrix4x4 objectToWorldMatrix = Matrix4x4.TRS(filterObj.transform.position, filterObj.transform.rotation, Vector3.one);
                Matrix4x4 camToObjectMatrix = objectToWorldMatrix.inverse * Camera.main.transform.localToWorldMatrix;
                MatrixToQuaternionTranslation(camToObjectMatrix, out Quaternion rotation, out Vector3 position);
                (rotation, position) = ConvertToOppositeHandedness(rotation, position);
                arPoseToInference = new float[] { rotation.w, rotation.x, rotation.y, rotation.z, position.x, position.y, position.z };
            }
        }

        public static void Display3DBox(string objName, Vector3 position, Quaternion rotation)
        {
            GameObject filterObj = GameObject.Find(objName);
            if (filterObj != null)//(objectInitialSet)
            {
                //if (objectInitialSet)
                //{
                //    Debug.Log(" Display3DBox objectInitialSet ");
                //    filterObj.transform.position = position;
                //}
                //else
                //{
                //    // Smooth movement
                //    Debug.Log(" Display3DBox Smooth movement ");
                //    filterObj.transform.position = Vector3.Lerp(filterObj.transform.position, position, 0.1f * Time.deltaTime);
                //}
                filterObj.transform.position = position;
                filterObj.transform.rotation = rotation;
                Debug.Log(rotation.eulerAngles.ToString());
            }
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

        //void ReadTXTFromFile(string filePath)
        //{
        //    string fileTxt = Application.dataPath + "/position_rotation.txt";
        //    StreamWriter writer = new StreamWriter(fileTxt);
        //    if (File.Exists(filePath))
        //    {
        //        //float[] positions = new float[59];
        //        bool unityGenerate = false;
        //        Vector3 objectScale = new Vector3(0.4f, 0.4f, 0.4f);
        //        var jsonContent = File.ReadAllLines(filePath);
        //        for (int i = 0; i < jsonContent.Length; i++)
        //        {
        //            string poseString = jsonContent[i];
        //            var stringSplitOrigin = poseString.Split(' ');
                    
        //            if (stringSplitOrigin.Length > 10)
        //            {
                        
        //                unityGenerate = true;
        //            }

        //            var stringSplit = RemoveFirstElementFromArray(stringSplitOrigin);

        //            if (!unityGenerate)
        //            {
        //                Quaternion rotation = new Quaternion(float.Parse(stringSplit[1]),
        //                                float.Parse(stringSplit[2]),
        //                                float.Parse(stringSplit[3]),
        //                                float.Parse(stringSplit[0])
        //                                );
        //                Vector3 position = new Vector3(float.Parse(stringSplit[4]),
        //                                                        float.Parse(stringSplit[5]),
        //                                                        float.Parse(stringSplit[6])
        //                                                        );

        //                GameObject nodeGameObject = Instantiate(originTransform, position, rotation);
        //                nodeGameObject.name = stringSplitOrigin[0];
        //                nodeGameObject.transform.SetParent(megaPose.transform);
        //                CamObjectMegaDictionary[stringSplitOrigin[0]] = nodeGameObject;

        //                (rotation, position) = ConvertToOppositeHandedness(rotation, position);

        //                Matrix4x4 CamToObjectMatrixMega = Matrix4x4.TRS(position, rotation, Vector3.one);//;

        //                Matrix4x4 ObjectToWorldMatrix = CamWorldDictionary[stringSplitOrigin[0]].transform.localToWorldMatrix * CamToObjectMatrixMega.inverse;

        //                MatrixToQuaternionTranslation(ObjectToWorldMatrix, out rotation, out position);

        //                //position = CamToObjectMatrixCal.inverse.MultiplyPoint(CamWorldDictionary[stringSplitOrigin[0]].transform.position);
        //                //Quaternion matrixRotation = Quaternion.LookRotation(CamToObjectMatrixCal.GetColumn(2), CamToObjectMatrixCal.GetColumn(1));
        //                //rotation = UnityEngine.Quaternion.Inverse(matrixRotation) * CamWorldDictionary[stringSplitOrigin[0]].transform.rotation;

        //                //position = CamToObjectMatrixCal.GetColumn(3);
        //                //position = position + CamWorldDictionary[stringSplitOrigin[0]].transform.position;
        //                GameObject nodeGameObject1 = Instantiate(originTransform, position, rotation);
        //                nodeGameObject1.transform.SetParent(boxOnWorldObject.transform);
        //                nodeGameObject1.name = stringSplitOrigin[0];

        //                // Compare distance:
        //                Debug.Log("AR distance" + stringSplitOrigin[0] + Vector3.Distance(CamObjectDictionary[stringSplitOrigin[0]].transform.position, CamWorldDictionary[stringSplitOrigin[0]].transform.position));

        //            }
        //            //position[0] = -position[0];
        //            else
        //            {
        //                Quaternion rotation = new Quaternion(float.Parse(stringSplit[8]),
        //                                float.Parse(stringSplit[9]),
        //                                float.Parse(stringSplit[10]),
        //                                float.Parse(stringSplit[7])
        //                                );
        //                Vector3 position = new Vector3(float.Parse(stringSplit[11]),
        //                                                        float.Parse(stringSplit[12]),
        //                                                        float.Parse(stringSplit[13])
        //                                                        );
        //                //GameObject nodeGameObject = Instantiate(originTransform, position, rotation);
        //                //nodeGameObject.transform.SetParent(arCam.transform);
        //                //nodeGameObject.name = stringSplitOrigin[0];
        //                //CamWorldDictionary[stringSplitOrigin[0]] = nodeGameObject;


        //                rotation = new Quaternion(float.Parse(stringSplit[1]),
        //                                        float.Parse(stringSplit[2]),
        //                                        float.Parse(stringSplit[3]),
        //                                        float.Parse(stringSplit[0])
        //                                                 );
        //                position = new Vector3(float.Parse(stringSplit[4]),
        //                                                        float.Parse(stringSplit[5]),
        //                                                        float.Parse(stringSplit[6])
        //                                                        );
        //                (rotation, position) = ConvertToOppositeHandedness(rotation, position);
        //                writer.WriteLine($"{stringSplitOrigin[0]} {rotation.w} {rotation.x} {rotation.y} {rotation.z} {position.x} {position.y} {position.z} \n");
        //                continue;


        //                //Matrix4x4 CamToWorldMatrix = CamWorldDictionary[stringSplitOrigin[0]].transform.localToWorldMatrix;

        //                //Matrix4x4 ObjectToWorldMatrix = box3D.transform.localToWorldMatrix;

        //                //Matrix4x4 CamToObjectMatrix = ObjectToWorldMatrix.inverse * CamToWorldMatrix;

        //                //position = CamToObjectMatrix.inverse.MultiplyPoint(CamWorldDictionary[stringSplitOrigin[0]].transform.position);
        //                //Quaternion matrixRotation = Quaternion.LookRotation(CamToObjectMatrix.GetColumn(2), CamToObjectMatrix.GetColumn(1));
        //                //rotation = UnityEngine.Quaternion.Inverse(matrixRotation) * CamWorldDictionary[stringSplitOrigin[0]].transform.rotation;

        //                //// Move object to 0 before calculate
        //                //Matrix4x4 boxOriginMatrix = Matrix4x4.TRS(Vector3.zero, box3D.transform.rotation, Vector3.one);
        //                //Matrix4x4 cameraOriginMatrix = Matrix4x4.TRS(CamWorldDictionary[stringSplitOrigin[0]].transform.position - box3D.transform.position, CamWorldDictionary[stringSplitOrigin[0]].transform.rotation, Vector3.one);
        //                //Matrix4x4 camToBoxMatrix = cameraOriginMatrix * boxOriginMatrix.inverse;


        //                //MatrixToQuaternionTranslation(camToBoxMatrix, out rotation, out position);
        //                GameObject nodeGameObject2 = Instantiate(originTransform, position, rotation);
        //                nodeGameObject2.transform.SetParent(camOnObject.transform);
        //                nodeGameObject2.name = stringSplitOrigin[0];
        //                CamObjectDictionary[stringSplitOrigin[0]] = nodeGameObject2;

        //            }
        //        }
        //    }
        //    else
        //    {
        //        Debug.LogError("JSON file not found at path: " + filePath);
        //    }
        //    writer.Close();
        //}

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
