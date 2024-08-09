using System;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.UI;
using System.Collections.Generic;
using static JsonDeserialization;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine.XR.ARFoundation.Samples;
using OpenCVForUnity.VideoModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.UtilsModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.DnnModule;
using static UnityEngine.XR.ARFoundation.Samples.DynamicLibrary;

public class ARCameraScript : MonoBehaviour
{
    [SerializeField] private Texture2D greenBBox;
    [SerializeField] private Texture2D redBBox;
    [SerializeField] private Texture2D grayBBox;
    [SerializeField] private Camera arCamera;
    [SerializeField] private RawImage resultStageDispalayImage;
    [SerializeField] private RawImage debugRawImage;
    [SerializeField] private Button nextStepBtn;
    [SerializeField] private GameObject captureBtn;
    [SerializeField] private int bBoxBorderSize = -1;
    [SerializeField] private TMPro.TextMeshProUGUI titleInfo;

    public GameObject sphere;
    //[SerializeField] private TMPro.TextMeshProUGUI logInfo;
    private Texture2D capturedTexture;
    private List<Datastage> dataStages;
    private GameObject checkListGameObject;
    private Transform checkMarkTransform;
    private RenderTexture renderTexture;
    private UnityEngine.Rect bBoxRect;
    private GUIStyle guiStyle = new GUIStyle();
    private Coroutine coroutineControler;
    private int reconnectedCount = 0;
    private byte[] CapturedImage;
    private Texture2D screenTexture;
    private RenderTexture screenRenderTexture;
    public static Texture2D resizeTextureOnnx;
    private RenderTexture resizeRenderTextureOnnx;
    public static bool inferenceResponseFlag = true;

    public Toggle toggleAP;
    public ComputeShader normalizeShader;
    private int width = 224;
    private int height = 224;
    private float[] mean = { 0.485f, 0.456f, 0.406f };
    private float[] std = { 0.229f, 0.224f, 0.225f };
    public static int inferenceClass = 0;
    public static int lastInferenceClass = 0;
    private List<int> smoothInference = Enumerable.Repeat(0, 20).ToList();
    public static float[] ImageFloatValues;
    public static float[] prb;
    public EdgeInferenceBarracuda edgeInference;
    //Stopwatch inferenceWatch = new Stopwatch();
    JsonDeserialization.InferenceResult metaAPIinferenceData;

    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private float savedFieldOfView;

    private void Start()
    {
        // Set the StationStageIndex FunctionIndex to "Home"
        StationStageIndex.FunctionIndex = "Home";

        // Subscribe to the OnInferenceResponse event in MetaApiStatic
        MetaService.OnInferenceResponse += OnInferenceResponse;

        EventManager.OnStageChange += ResetBoundingBox;

        // Create a new RenderTexture with specified dimensions
        renderTexture = new RenderTexture(MetaService.imageWidth2Meta, MetaService.imageHeight2Meta, 24);
        screenRenderTexture = new RenderTexture(Screen.width, Screen.height, 24);

        // Create a new Texture2D with specified dimensions and format
        capturedTexture = new Texture2D(MetaService.imageWidth2Meta, MetaService.imageHeight2Meta, TextureFormat.RGB24, false);
        screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        //network = new NNetwork(modelAsset);
        EventManager.OnCheckpointUpdateEvent += Inference;

        resizeTextureOnnx = new Texture2D(width, height);
        resizeRenderTextureOnnx = new RenderTexture(width, height, 24);

        // Set the size of the resultStageDispalayImage's RectTransform to match the screen size
        resultStageDispalayImage.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);

        // Set the border of the GUI style with a specified size
        guiStyle.border = new RectOffset(bBoxBorderSize, bBoxBorderSize, bBoxBorderSize, bBoxBorderSize);
    }

    private void ResetBoundingBox(object sender, EventManager.OnStageIndexEventArgs e)
    {
        // Reset bbox when stage change
        guiStyle.normal.background = grayBBox;
        smoothInference = Enumerable.Repeat(0, 20).ToList();
    }

        private void OnInferenceResponse(object sender, MetaService.OnInferenceResponseEventArgs e)
    {
        // Listen event from Inference response in Meta API, event parameter will be drawn in this function
        UnityEngine.Debug.Log("START ARCameraScript/OnInferenceResponse ");

        // Check if the function index is not "Detect"
        if (StationStageIndex.FunctionIndex != "Detect")
        {
            inferenceResponseFlag = true;
            return;
        }

        try
        {
            // Deserialize the inference response
            metaAPIinferenceData = JsonConvert.DeserializeObject<JsonDeserialization.InferenceResult>(e.inferenceResponse);

            // Check if rule data is not null
            if (!metaAPIinferenceData.data.rule.Equals(null))
            {
                Vector3 centerPoint; float radiusOnScreen; Vector3 centerPoint3D;
                (centerPoint, radiusOnScreen, centerPoint3D) = GetObjectCenterRadiusBaseAI();
                sphere.transform.position = centerPoint3D;

                // Set Detection result
                if (!StationStageIndex.metaInferenceRule && metaAPIinferenceData.data.rule)
                {
                    StationStageIndex.metaInferenceRule = metaAPIinferenceData.data.rule;
                    dataStages = ConfigRead.configData.DataStation[StationStageIndex.stationIndex].Datastage;

                    // Show/hide next step and capture buttons based on current stage
                    if (StationStageIndex.stageIndex < dataStages.Count - 1)
                    {
                        nextStepBtn.gameObject.SetActive(true);
                        captureBtn.gameObject.SetActive(false);
                    }

                    // Stop the metaTimeCount if it's not null
                    if (StationStageIndex.metaTimeCount != null)
                    {
                        StationStageIndex.metaTimeCount.Stop();
                    }

                    // Find the checkListGameObject and show the checkmark
                    checkListGameObject = GameObject.Find("CP" + StationStageIndex.stageIndex.ToString());
                    checkMarkTransform = checkListGameObject.transform.Find("Background").transform.Find("Checkmark");
                    checkMarkTransform.gameObject.SetActive(true);
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("ERROR ARCameraScript/OnInferenceResponse" + ex.ToString());
        }

        // Clean up and set inferenceResponseFlag to true
        e = null;
        inferenceResponseFlag = true;

        UnityEngine.Debug.Log("END ARCameraScript/OnInferenceResponse ");
    }

    public (Vector3, float) GetObjectCenterRadiusBaseAR()
    {
        // Get checkpoint position and scale
        Vector3 thisCheckpointPosition = StationStageIndex.gameObjectPoints[$"{StationStageIndex.stageIndex}"][0];
        Vector3 thisCheckpointScale = StationStageIndex.gameObjectPoints[$"{StationStageIndex.stageIndex}"][1];

        // Calculate normal vectors and line direction
        Vector3 normalVector1 = GeometryUtils.CalculateNormal(thisCheckpointPosition,
                        arCamera.transform.position,
                        thisCheckpointPosition + new Vector3(0f, thisCheckpointScale.x / 2, 0f));
        Vector3 normalVector2 = arCamera.transform.position - thisCheckpointPosition;
        Vector3 lineDirection = Vector3.Cross(normalVector1, normalVector2).normalized;

        // Calculate screen positions
        Vector3 topPosition = GetScreenSpacePoint(thisCheckpointPosition + lineDirection * thisCheckpointScale.y / 2);
        Vector3 centerPoint = GetScreenSpacePoint(thisCheckpointPosition);

        // Calculate radius on screen and bounding box position
        float radiusOnScreen = Vector2.Distance(topPosition, centerPoint);
        return (centerPoint, radiusOnScreen);
    }

    void SaveCameraState()
    {
        if (Camera.main != null)
        {
            savedPosition = Camera.main.transform.position;
            savedRotation = Camera.main.transform.rotation;
            savedFieldOfView = Camera.main.fieldOfView;
        }
    }

    public (Vector3, float, Vector3) GetObjectCenterRadiusBaseAI()
    {
        if (metaAPIinferenceData != null && metaAPIinferenceData.data.rois.Count > 0)
        {
            int x1 = metaAPIinferenceData.data.rois[0][0];
            int y1 = metaAPIinferenceData.data.rois[0][1];
            int x2 = metaAPIinferenceData.data.rois[0][2];
            int y2 = metaAPIinferenceData.data.rois[0][3];

            Vector2 centerPoint2D = new Vector3((x1 + x2) / 2,(y1 + y2) / 2);

            //Convert cpuImage point to ScreenPoint
            Vector2 screenPoint = Vector2.zero;
            Vector2 xrImageSize = new Vector2(TrackedImageInfoManager.cpuImageTexture.width, TrackedImageInfoManager.cpuImageTexture.height);
            Vector2 ScreenImageSize = new Vector2(Screen.width, Screen.height);
            ImageProcessing.XrImagePointToScreenPoint(centerPoint2D, out screenPoint, xrImageSize, ScreenImageSize);

            centerPoint2D = screenPoint;
            float depth = 0.1f;
            // Only available on phone
#if !UNITY_EDITOR
            //Get depth and convert 3d
            Vector2 depthImageSize = new Vector2(PointCloudTracking.texture.width, PointCloudTracking.texture.height);
            Vector2 depthPoint = Vector2.zero;
            ImageProcessing.XrImagePointToScreenPoint(centerPoint2D, out depthPoint, xrImageSize, depthImageSize);

            depth = ReadDepthValue(PointCloudTracking.texture,
                (int)(depthImageSize.x - depthPoint.x),
                (int)(depthImageSize.y - depthPoint.y));
#endif

            Camera tempCamera = new GameObject("TempCamera").AddComponent<Camera>();
            // Set the temporary camera's properties to the saved state
            tempCamera.transform.position = savedPosition;
            tempCamera.transform.rotation = savedRotation;
            tempCamera.fieldOfView = savedFieldOfView;
            Vector3 centerPoint3D = tempCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, Screen.height - screenPoint.y, depth));

            Destroy(tempCamera);

            float radiusOnScreen = (x2 - x1)*Screen.width/ (2*xrImageSize.x);
            return (centerPoint2D, radiusOnScreen, centerPoint3D);
        }
        else
        {
            return (Vector3.zero, 1f, Vector3.zero);
        }
        
    }

    public UnityEngine.Rect GetObjectBBox()
    {
        Vector3 centerPoint; float radiusOnScreen;Vector3 centerPoint3D;
        (centerPoint, radiusOnScreen, centerPoint3D) = GetObjectCenterRadiusBaseAI();
        Vector3 corner1World2D = centerPoint - new Vector3(radiusOnScreen, radiusOnScreen, 0f);
        UnityEngine.Rect unityRect = new UnityEngine.Rect((int)(corner1World2D[0]), (int)(corner1World2D[1]), radiusOnScreen * 2, radiusOnScreen * 2);
        return unityRect;
    }

    public void DrawRois(bool drawOnResultStage)
    {

        if (!drawOnResultStage)
        {
            // Update bounding box position
            bBoxRect = GetObjectBBox();
        }

        // Set GUI style and label based on meta inference rule
        if (toggleAP.isOn)
        {
            if (inferenceClass == 0)
            {
                UpdateIfClassChange(grayBBox, false);
                return;
            }

            bool allElementsAreSame = smoothInference.All(element => element == inferenceClass);
            if (allElementsAreSame)
            {
                if (lastInferenceClass != inferenceClass)
                {
                    lastInferenceClass = inferenceClass;
                    if (StationStageIndex.stageIndex * 2 - 1 == inferenceClass)// odd class is green
                    {
                        UpdateIfClassChange(greenBBox, true);
                    }
                    else if (StationStageIndex.stageIndex * 2 == inferenceClass)// even class is red
                    {
                        UpdateIfClassChange(redBBox, false);
                    }
                    else
                    {
                        UpdateIfClassChange(grayBBox, false);// original is gray
                    }
                }
            }
            else
            {
                smoothInference.RemoveAt(0);
                //if (StationStageIndex.stageIndex * 2 - 1 != inferenceClass && StationStageIndex.stageIndex * 2 != inferenceClass)
                //{
                //    smoothInference.Add(0);
                //}
                //else { smoothInference.Add(inferenceClass); }
                smoothInference.Add(inferenceClass);
            }
        }
        else
        {
            if (StationStageIndex.metaInferenceRule)
            {
                guiStyle.normal.background = greenBBox;
            }
            else
            {
                guiStyle.normal.background = redBBox;
            }
        }
        //GUI.depth = 3;
        // Draw bounding box
        GUI.Box(bBoxRect, "", guiStyle);

    }

    void UpdateIfClassChange(Texture2D bboxTexture, bool isPass)
    {
        guiStyle.normal.background = bboxTexture;
        if (StationStageIndex.stageIndex < 4)
        {
            nextStepBtn.gameObject.SetActive(isPass);
            captureBtn.gameObject.SetActive(!isPass);
        }
        checkListGameObject = GameObject.Find("CP" + StationStageIndex.stageIndex.ToString());
        checkMarkTransform = checkListGameObject.transform.Find("Background").transform.Find("Checkmark");
        checkMarkTransform.gameObject.SetActive(isPass);
    }

    void OnGUI_()
    {
        if (StationStageIndex.FunctionIndex == "Detect") {
            DrawRois(false);
        }
        else if (StationStageIndex.FunctionIndex == "Result") {
            DrawRois(true);
        }
        else if (StationStageIndex.FunctionIndex == "Sample")
        {
            smoothInference = Enumerable.Repeat(0, 20).ToList();
        }
    }

    private void Update()
    {
        // Check if the current function index is "Detect"
        if (StationStageIndex.FunctionIndex == "Detect" && !EdgeInferenceBarracuda.isSpeedRealCameraARFast(.05f))
        {
            // Ignore object out of view
            Vector3 screenPoint = arCamera.WorldToViewportPoint(StationStageIndex.stagePosition);
            //if (screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1 || screenPoint.z < 0)
            //{
            //    //logInfo.text = "return";
            //    return;
            //}
            //Update stage position
            EventManager.OnCheckpointUpdateEvent?.Invoke(this, new EventManager.OnCheckpointUpdateEventArgs
            {
                status = true
            });
            // Set the titleInfo text to display elapsed minutes and seconds from metaTimeCount
            titleInfo.text = $"{StationStageIndex.metaTimeCount.Elapsed.Minutes}:{StationStageIndex.metaTimeCount.Elapsed.Seconds}";// +  " " + usedMemory / 1024000 + " MB";
        }
        else
        {
            // Reset titleInfo text if the function index is not "Detect"
            titleInfo.text = "";
        }
    }



    private void Inference(object sender, EventManager.OnCheckpointUpdateEventArgs e)
    {
        //logInfo.text = "Update start:";
        UnityEngine.Debug.Log("Update start:");
        // Edge inference
        if (toggleAP.isOn)
        {
            //UnityEngine.Debug.Log("toggleAP.isOn: start");
            if (inferenceResponseFlag)
            {
                //inferenceWatch.Restart();
                GetOutputTensor();
                UnityEngine.Debug.Log("toggleAP.isOn: inferenceResponseFlag");
                inferenceResponseFlag = false;
                EdgeInferenceAsync();
                //StartCoroutine(EdgeInferenceAsync());
            }
            //UnityEngine.Debug.Log("toggleAP.isOn: finish");
        }
        // If inferenceResponseFlag is true, perform necessary actions
        else if (inferenceResponseFlag)
        {
            UnityEngine.Debug.Log("else if (inferenceResponseFlag)");
            // Check if triggerAPIresponseData.result is null or false then reconnect
            if (MetaService.stageData == null)
            {
                //logInfo.text = "Reconnect triggerAPIresponseData" + MetaService.stageData;
                reconnectedCount++;

                // If reconnectedCount exceeds 100, reset it and connect to Meta based on project ID
                if (reconnectedCount > 100)
                {
                    reconnectedCount = 0;
                    MetaService.ConnectWithMetaProjectID();
                }

                // Connect to Meta based on stage
                MetaService.ConnectWithMetaStageID();
                return;
            }
            if (StationStageIndex.FunctionIndex == "Detect")
            {
                inferenceResponseFlag = false;
                SendImage2Meta();
            }

            //Debug.Log("Object is within the screen's view.");
        }
        // Check if inference may stop
        //inferenceWatch.Stop();
        //if (inferenceWatch.ElapsedMilliseconds > 300)
        //{
        //    inferenceResponseFlag = true;
        //    //logInfo.text += "===========reset===========";
        //}
        //else
        //{
        //    inferenceWatch.Start();
        //}
    }
    //Texture2D resizeTexture;
    private void EdgeInferenceAsync()
    {
        UnityEngine.Debug.Log("toggleAP.isOn: inferenceResponseFlag EdgeInferenceAsync");
        //Stopwatch stopwatch = new Stopwatch(); stopwatch.Start();

        //Crop Image
        float bboxX = bBoxRect.x;// + bBoxRect.width/2);
#if UNITY_EDITOR
        float bboxY = (float)(Screen.height - bBoxRect.y - bBoxRect.height);
#else
        float bboxY = (float)(Screen.height- bBoxRect.y- bBoxRect.height);
#endif
        float bboxW = (float)bBoxRect.width;
        float bboxH = (float)bBoxRect.height;
        if (bboxW <= 0 || bboxH <= 0 || bboxX <= 0 || bboxX + bboxW >= Screen.width || bboxY <= 0 || bboxY + bboxH >= Screen.height)
        {
            //logInfo.text = "Return" + bboxW.ToString() + " " + bboxH.ToString() + " " + bboxX.ToString() + " " + bboxY.ToString();
            inferenceResponseFlag = true;
            return ;
        }
        try
        {
            // Crop image from cpu image
            if (TrackedImageInfoManager.cpuImageTexture != null)
            {
                int cpuWidth = TrackedImageInfoManager.cpuImageTexture.width;
                int cpuHeight = TrackedImageInfoManager.cpuImageTexture.height;
                Vector2 cpuImageSize = new Vector2(TrackedImageInfoManager.cpuImageTexture.width, TrackedImageInfoManager.cpuImageTexture.height);
                Vector2 screenImageSize = new Vector2(Screen.width, Screen.height);
                Vector2 screenStartPointInCpuImage;
                ImageProcessing.ScreenPointToXrImagePoint(Vector2.zero,
                                            out screenStartPointInCpuImage,
                                            cpuImageSize,
                                            screenImageSize);
                UnityEngine.Rect newRect = new UnityEngine.Rect((int)((bboxX / Screen.width) * cpuWidth),
                    (int)((bboxY / Screen.height) * (cpuWidth * Screen.height / Screen.width) + screenStartPointInCpuImage.y),
                    (int)(bboxW * cpuWidth / Screen.width),
                    (int)(bboxW * cpuWidth / Screen.width));
                Texture2D croppedTexture = new Texture2D((int)newRect.width, (int)newRect.height);
                croppedTexture = ImageProcessing.CropTexture2D(TrackedImageInfoManager.cpuImageTexture,
                    croppedTexture, newRect
                    );

                //stopwatch.Stop(); long elMs = stopwatch.ElapsedMilliseconds; stopwatch.Reset(); stopwatch.Start();
                //logInfo.text = "croppedTexture time:" + elMs + "ms \n";

                //File.WriteAllBytes("test.jpg", croppedTexture.EncodeToJPG());

                //if (resizeTexture == null)
                //{
                //    resizeTexture = new Texture2D(224, 224); //ResizeTextureOnnx(croppedTexture);
                //}

                Mat resizedMat = new Mat(croppedTexture.height, croppedTexture.width, CvType.CV_8UC4);
                Utils.texture2DToMat(croppedTexture, resizedMat);

                //resizedMat = Imgcodecs.imread("/Users/hungnguyencong/Downloads/2_20230912150544983.jpg", Imgcodecs.IMREAD_UNCHANGED);

                // Resize the Mat
                Imgproc.resize(resizedMat, resizedMat, new Size(width, height));

                //Imgproc.cvtColor(resizedMat, resizedMat, Imgproc.COLOR_RGB2BGR);

                //resizedMat = NormalizeImage(resizedMat);

                
                Utils.matToTexture2D(resizedMat, resizeTextureOnnx);
#if UNITY_EDITOR
                string filePath = Path.Combine(Application.persistentDataPath, "test.jpg");
                System.IO.File.WriteAllBytes(filePath, resizeTextureOnnx.EncodeToJPG());
                //System.IO.File.WriteAllBytes("test.jpg", resizeTextureOnnx.EncodeToJPG());
#endif
                //stopwatch.Stop(); elMs = stopwatch.ElapsedMilliseconds; stopwatch.Reset(); stopwatch.Start();
                //logInfo.text += "resize time:" + elMs + "ms \n";

                ImageFloatValues = NormalizeImageWithComputeShader(resizeTextureOnnx);

                Destroy(croppedTexture);
            }

            UnityEngine.Debug.Log("toggleAP.isOn: inferenceResponseFlag EdgeInferenceAsync finish");
        }
        catch (Exception ex)
        {
            //logInfo.text = ex.Message;
            UnityEngine.Debug.LogError( ex.Message);
        }
    }

    //Mat NormalizeImage(Mat imageMat)
    //{
    //    Mat normalizedMat = new Mat();
    //    Scalar mean = new Scalar(0.485 * 255, 0.456 * 255, 0.406 * 255);
    //    Scalar std = new Scalar(0.229 * 255, 0.224 * 255, 0.225 * 255);

    //    Core.subtract(imageMat, mean, normalizedMat);
    //    Core.divide(normalizedMat, std, normalizedMat);
    //    //Core.normalize(normalizedMat, normalizedMat, 0, 255, Core.NORM_MINMAX);

    //    return normalizedMat;
    //}

    Mat NormalizeImage(Mat inputMat)
    {
        Mat floatMat = new Mat();
        inputMat.convertTo(floatMat, CvType.CV_32FC3);

        // Normalize the image
        Scalar mean = new Scalar(0.485, 0.456, 0.406);
        Scalar std = new Scalar(0.229, 0.224, 0.225);

        Core.subtract(floatMat, mean, floatMat);
        Core.divide(floatMat, std, floatMat);

        // Convert back to original type
        floatMat.convertTo(inputMat, CvType.CV_8UC3);

        return inputMat;
    }

    void GetOutputTensor()
    {
        if (prb != null)
        {
            //float[] prb = outputTensor.ToReadOnlyArray();
            //float[] prb = outputTensor.ToReadOnlyArray();
            //outputTensor.Dispose();
            inferenceClass = GetMaxValueIndex(prb) + 1;
        }
    }

    //Texture2D ResizeTextureOnnx(Texture2D texture2D)
    //{
    //    //result = null;
    //    RenderTexture.active = resizeRenderTextureOnnx;
    //    Graphics.Blit(texture2D, resizeRenderTextureOnnx);
    //    resizeTextureOnnx.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
    //    resizeTextureOnnx.Apply();
    //    RenderTexture.active = null;
    //    return resizeTextureOnnx;
    //}

    float[] NormalizeImageWithComputeShader(Texture2D imageTexture)
    {
        int kernelIndex = normalizeShader.FindKernel("ComputeNormalization");

        ComputeBuffer outputBuffer = new ComputeBuffer(width * height, sizeof(float) * 3);
        normalizeShader.SetTexture(kernelIndex, "InputImage", imageTexture);
        normalizeShader.SetBuffer(kernelIndex, "OutputBuffer", outputBuffer);
        normalizeShader.SetFloats("Mean", mean);
        normalizeShader.SetFloats("Std", std);
        normalizeShader.SetVector("TextureDimensions", new Vector2(imageTexture.width, imageTexture.height)); // Set texture dimensions

        uint maxThreadGroupSizeX, maxThreadGroupSizeY, maxThreadGroupSizeZ;
        normalizeShader.GetKernelThreadGroupSizes(kernelIndex, out maxThreadGroupSizeX, out maxThreadGroupSizeY, out maxThreadGroupSizeZ);

        int threadGroupsX = (int)Mathf.CeilToInt(imageTexture.width / maxThreadGroupSizeX); // Adjust thread group size as needed
        int threadGroupsY = (int)Mathf.CeilToInt(imageTexture.height / maxThreadGroupSizeY);

        normalizeShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);

        float[] floatValues = new float[imageTexture.width * imageTexture.height * 3];
        outputBuffer.GetData(floatValues);

        outputBuffer.Release();
        return floatValues;
        // Now you have the normalized floatValues array
    }


    public float[] NormalizeImage(Texture2D image)
    {
        float[] floatValues = new float[width * height * 3];

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = image.GetPixel(x, y);
                int index = (height - y - 1) * width + x;
                floatValues[index * 3 + 0] = (color.r - mean[0]) / std[0];
                floatValues[index * 3 + 1] = (color.g - mean[1]) / std[1];
                floatValues[index * 3 + 2] = (color.b - mean[2]) / std[2];
            }
        }
        return floatValues;
    }

    private int GetMaxValueIndex(float[] array)
    {
        if (array.Length == 0)
        {
            //Debug.LogError("Array is empty");
            return -1; // Return an invalid index as a flag
        }

        float maxValue = array[0];
        int maxIndex = 0;

        for (int i = 1; i < array.Length; i++)
        {
            if (array[i] > maxValue)
            {
                maxValue = array[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    private void SendImage2Meta()
    {
        //// Set the target texture of the AR camera to the render texture
        //arCamera.targetTexture = renderTexture;

        //// Render the AR camera
        //arCamera.Render();

        //// Set the active render texture
        //RenderTexture.active = renderTexture;

        //// Read the pixels from the specified rectangle in the capture texture
        //capturedTexture.ReadPixels(new UnityEngine.Rect(0, 0, MetaService.imageWidth2Meta, MetaService.imageHeight2Meta), 0, 0);

        //// Apply the changes made to the capture texture
        //capturedTexture.Apply();

        // Encode the capture texture as JPG and assign it to the CapturedImage variable
        CapturedImage = TrackedImageInfoManager.cpuImageTexture.EncodeToJPG();//capturedTexture.EncodeToJPG();

        //Also get depth image for convert 2D to 3D
        PointCloudTracking.uploadDepthImage = true;

        //Get current camera pose
        SaveCameraState();

#if UNITY_EDITOR
        File.WriteAllBytes("meta.jpg", CapturedImage);
#endif
        // Check if there is a coroutine already running and stop it
        if (coroutineControler != null)
        {
            StopCoroutine(coroutineControler);
        }

        // Start a new coroutine for the InferenceAPI using the captured image
        coroutineControler = StartCoroutine(MetaService.InferenceAPI(CapturedImage));

        // Reset the target and active render textures
        //arCamera.targetTexture = null;
        //RenderTexture.active = null;
    }

    private float ReadDepthValue(Texture2D depthTexture, int x, int y)
    {
        try
        {
            // Convert the pixel coordinates to the corresponding UV coordinates
            Vector2 uv = new Vector2(x / (float)depthTexture.width, y / (float)depthTexture.height);

            // Read the depth value at the UV coordinates from the depth texture
            float depthValue = depthTexture.GetPixelBilinear(uv.x, uv.y).r;

            return depthValue;
        }
        catch
        {
            return 0.5f;
        }

    }

    // Takes a screenshot and displays it on the result stage display image
    public void TakeScreenshot()
    {
        // Assign the captured texture to the result stage display image texture
        if (toggleAP.isOn)
        {
            // Set the target texture of the AR camera to the render texture
            arCamera.targetTexture = renderTexture;

            // Render the AR camera
            arCamera.Render();

            // Set the active render texture
            RenderTexture.active = renderTexture;

            // Read the pixels from the specified rectangle in the capture texture
            capturedTexture.ReadPixels(new UnityEngine.Rect(0, 0, MetaService.imageWidth2Meta, MetaService.imageHeight2Meta), 0, 0);

            // Apply the changes made to the capture texture
            capturedTexture.Apply();
            arCamera.targetTexture = null;
            RenderTexture.active = null;
        }
        resultStageDispalayImage.texture = capturedTexture;

    }

    private Vector3 GetScreenSpacePoint(Vector3 worldPosition)
    {
        // Convert the world position to screen space point
        Vector3 screenSpacePoint = arCamera.WorldToScreenPoint(worldPosition);

        // Invert the y-coordinate to match the screen space (0,0) at the bottom left
        screenSpacePoint.y = Screen.height - screenSpacePoint.y;

        return screenSpacePoint;
    }
}