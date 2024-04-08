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

public class ARCameraScript : MonoBehaviour
{
    [SerializeField] private Texture2D greenBBox;
    [SerializeField] private Texture2D redBBox;
    [SerializeField] private Texture2D grayBBox;
    [SerializeField] private Camera arCamera;
    [SerializeField] private RawImage resultStageDispalayImage;
    [SerializeField] private Button nextStepBtn;
    [SerializeField] private GameObject captureBtn;
    [SerializeField] private int bBoxBorderSize = -1;
    [SerializeField] private TMPro.TextMeshProUGUI titleInfo;
    [SerializeField] private TMPro.TextMeshProUGUI logInfo;
    private Texture2D capturedTexture;
    private List<Datastage> dataStages;
    private GameObject checkListGameObject;
    private Transform checkMarkTransform;
    private RenderTexture renderTexture;
    private Rect bBoxRect;
    private GUIStyle guiStyle = new GUIStyle();
    private Coroutine coroutineControler;
    private int reconnectedCount = 0;
    private byte[] CapturedImage;
    private Texture2D screenTexture;
    private RenderTexture screenRenderTexture;
    private Texture2D resizeTextureOnnx;
    private RenderTexture resizeRenderTextureOnnx;
    public static bool inferenceResponseFlag = true;

    public Toggle toggleAP;
    public ComputeShader normalizeShader;
    private int width = 224;
    private int height = 224;
    private float[] mean = { 0.485f, 0.456f, 0.406f };
    private float[] std = { 0.229f, 0.224f, 0.225f };
    public static int inferenceClass = 0;
    private int lastInferenceClass = 0;
    private List<int> smoothInference = Enumerable.Repeat(0, 20).ToList();
    public static float[] ImageFloatValues;
    public static float[] prb;
    public EdgeInferenceBarracuda edgeInference;
    Stopwatch inferenceWatch = new Stopwatch();
    private void Start()
    {
        // Set the StationStageIndex FunctionIndex to "Home"
        StationStageIndex.FunctionIndex = "Home";

        // Subscribe to the OnInferenceResponse event in MetaApiStatic
        MetaService.OnInferenceResponse += OnInferenceResponse;

        //EventManager.OnStageChange += OnUpdate3DModelName;

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

    private void OnInferenceResponse(object sender, MetaService.OnInferenceResponseEventArgs e)
    {
        // Listen event from Inference response in Meta API, event parameter will be drawn in this function
        UnityEngine.Debug.Log("START ARCameraScript/OnInferenceResponse ");

        // Check if the function index is not "Detect"
        if (StationStageIndex.FunctionIndex != "Detect")
        {
            return;
        }

        try
        {
            // Deserialize the inference response
            InferenceResult metaAPIinferenceData = JsonConvert.DeserializeObject<InferenceResult>(e.inferenceResponse);

            // Check if rule data is not null
            if (!metaAPIinferenceData.data.rule.Equals(null))
            {
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

    public (Vector3, float) GetObjectCenterRadius()
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

    public Rect GetObjectBBox()
    {
        Vector3 centerPoint; float radiusOnScreen;
        (centerPoint, radiusOnScreen) = GetObjectCenterRadius();
        Vector3 corner1World2D = centerPoint - new Vector3(radiusOnScreen, radiusOnScreen, 0f);
        Rect unityRect = new Rect((int)(corner1World2D[0]), (int)(corner1World2D[1]), radiusOnScreen * 2, radiusOnScreen * 2);
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
                UpdateIfClassChange(redBBox, false);
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
                        UpdateIfClassChange(grayBBox, false);
                    }
                }
            }
            else
            {
                smoothInference.RemoveAt(0);
                if (StationStageIndex.stageIndex * 2 - 1 != inferenceClass && StationStageIndex.stageIndex * 2 != inferenceClass)
                {
                    smoothInference.Add(0);
                }
                else { smoothInference.Add(inferenceClass); }
                
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

    void OnGUI()
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
        if (StationStageIndex.FunctionIndex == "Detect")
        {
            // Ignore object out of view
            Vector3 screenPoint = arCamera.WorldToViewportPoint(StationStageIndex.stagePosition);
            if (screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1 || screenPoint.z < 0)
            {
                logInfo.text = "return";
                return;
            }
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
            UnityEngine.Debug.Log("toggleAP.isOn: start");
            if (inferenceResponseFlag)
            {
                inferenceWatch.Restart();
                GetOutputTensor();
                UnityEngine.Debug.Log("toggleAP.isOn: inferenceResponseFlag");
                inferenceResponseFlag = false;
                EdgeInferenceAsync();
                //StartCoroutine(EdgeInferenceAsync());
            }
            UnityEngine.Debug.Log("toggleAP.isOn: finish");
        }
        // If inferenceResponseFlag is true, perform necessary actions
        else if (inferenceResponseFlag)
        {
            UnityEngine.Debug.Log("else if (inferenceResponseFlag)");
            // Check if triggerAPIresponseData.result is null or false then reconnect
            if (MetaService.stageData == null || !MetaService.stageData.requestResult)
            {
                logInfo.text = "Reconnect triggerAPIresponseData" + MetaService.stageData;
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

            inferenceResponseFlag = false;
            SendImage2Meta();
            //Debug.Log("Object is within the screen's view.");
        }
        // Check if inference may stop
        inferenceWatch.Stop();
        if (inferenceWatch.ElapsedMilliseconds > 300)
        {
            inferenceResponseFlag = true;
            logInfo.text += "===========reset===========";
        }
        else
        {
            inferenceWatch.Start();
        }
    }

    private void EdgeInferenceAsync()
    {
        UnityEngine.Debug.Log("toggleAP.isOn: inferenceResponseFlag EdgeInferenceAsync");
        Stopwatch stopwatch = new Stopwatch(); stopwatch.Start();
        // Set the target texture of the AR camera to the render texture
        arCamera.targetTexture = screenRenderTexture;

        // Render the AR camera
        arCamera.Render();

        // Set the active render texture
        RenderTexture.active = screenRenderTexture;

        //Crop Image
        int bboxX = (int)(bBoxRect.x);// + bBoxRect.width/2);
#if UNITY_EDITOR
        int bboxY = (int)bBoxRect.y;
#else
        int bboxY = (int)(Screen.height- bBoxRect.y- bBoxRect.height);
#endif
        int bboxW = (int)bBoxRect.width;
        int bboxH = (int)bBoxRect.height;
        if (bboxW <= 0 || bboxH <= 0 || bboxX <= 0 || bboxX + bboxW >= Screen.width || bboxY <= 0 || bboxY + bboxH >= Screen.height)
        {
            logInfo.text = "Return";
            arCamera.targetTexture = null;
            RenderTexture.active = null;
            inferenceResponseFlag = true;
            return ;
        }
        try
        {
            Texture2D croppedTexture = new Texture2D(bboxW, bboxH);
            croppedTexture.ReadPixels(new Rect(bboxX, bboxY, bboxW, bboxH), 0, 0);
            croppedTexture.Apply();

            arCamera.targetTexture = null;
            RenderTexture.active = null;

            stopwatch.Stop(); long elMs = stopwatch.ElapsedMilliseconds; stopwatch.Reset(); stopwatch.Start();
            //logInfo.text = "croppedTexture time:" + elMs + "ms \n";

            Texture2D resizeTexture = ResizeTextureOnnx(croppedTexture);
            stopwatch.Stop(); elMs = stopwatch.ElapsedMilliseconds; stopwatch.Reset(); stopwatch.Start();
            //logInfo.text += "resize time:" + elMs + "ms \n";

            ImageFloatValues = NormalizeImageWithComputeShader(resizeTexture);

            //edgeInference.StartContinuousInference();
            //stopwatch.Stop(); elMs = stopwatch.ElapsedMilliseconds; stopwatch.Reset(); stopwatch.Start();
            //logInfo.text += "normalize time:" + elMs + "ms \n";

            //Tensor inputTensor = new Tensor(1, height, width, 3, ImageFloatValues);
            //await network.ForwardAsyncAlt(inputTensor);
            //logInfo.text = inferenceClass.ToString();

            Destroy(croppedTexture);
            //inputTensor.Dispose();
            stopwatch.Stop(); elMs = stopwatch.ElapsedMilliseconds; //UnityEngine.Debug.Log("NormalizeImage time:" + elMs + "ms");
            //logInfo.text += "inference time:" + elMs + "ms \n";

            UnityEngine.Debug.Log("toggleAP.isOn: inferenceResponseFlag EdgeInferenceAsync finish");
        }
        catch (Exception ex)
        {
            logInfo.text = ex.Message;
            UnityEngine.Debug.LogError( ex.Message);
        }
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

    Texture2D ResizeTextureOnnx(Texture2D texture2D)
    {
        //result = null;
        RenderTexture.active = resizeRenderTextureOnnx;
        Graphics.Blit(texture2D, resizeRenderTextureOnnx);
        resizeTextureOnnx.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resizeTextureOnnx.Apply();
        RenderTexture.active = null;
        return resizeTextureOnnx;
    }

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

    Texture2D NormalizeImageWithComputeShaderOutTexture(Texture2D imageTexture)
    {
        int kernelIndex = normalizeShader.FindKernel("ComputeNormalizationOutTexture");

        RenderTexture outputTexture = new RenderTexture(imageTexture.width, imageTexture.height, 0, RenderTextureFormat.ARGBFloat);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        normalizeShader.SetTexture(kernelIndex, "InputImage", imageTexture);
        normalizeShader.SetTexture(kernelIndex, "OutputTexture", outputTexture);
        normalizeShader.SetFloats("Mean", mean);
        normalizeShader.SetFloats("Std", std);
        normalizeShader.SetVector("TextureDimensions", new Vector2(imageTexture.width, imageTexture.height));

        uint maxThreadGroupSizeX, maxThreadGroupSizeY, maxThreadGroupSizeZ;
        normalizeShader.GetKernelThreadGroupSizes(kernelIndex, out maxThreadGroupSizeX, out maxThreadGroupSizeY, out maxThreadGroupSizeZ);

        int threadGroupsX = (int)Mathf.CeilToInt(imageTexture.width / maxThreadGroupSizeX);
        int threadGroupsY = (int)Mathf.CeilToInt(imageTexture.height / maxThreadGroupSizeY);

        normalizeShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);

        // Create a new Texture2D and read the RenderTexture data into it
        Texture2D resultTexture = new Texture2D(imageTexture.width, imageTexture.height, TextureFormat.RGBAFloat, false);
        RenderTexture.active = outputTexture;
        resultTexture.ReadPixels(new Rect(0, 0, outputTexture.width, outputTexture.height), 0, 0);
        resultTexture.Apply();
        RenderTexture.active = null;
        // Memory leak in resultTexture
        // Release the RenderTexture
        outputTexture.Release();

        return resultTexture;
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

    //public float[] RunInference(float[] inputImageData)
    //{

    //    // Preprocess input texture if needed
    //    Tensor inputTensor = new Tensor(1, height, width, 3, inputImageData);

    //    // Perform inference
    //    worker.Execute(inputTensor);

    //    // Retrieve the output tensor(s) and process the results
    //    Tensor outputTensor = worker.PeekOutput();

    //    // Post-process output tensor to get results
    //    float[] classProbabilities = outputTensor.ToReadOnlyArray();
    //    // Clean up resources
    //    inputTensor.Dispose();
    //    outputTensor.Dispose();

    //    // Stop the stopwatch

    //    return classProbabilities;
    //}

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
        // Set the target texture of the AR camera to the render texture
        arCamera.targetTexture = renderTexture;

        // Render the AR camera
        arCamera.Render();

        // Set the active render texture
        RenderTexture.active = renderTexture;

        // Read the pixels from the specified rectangle in the capture texture
        capturedTexture.ReadPixels(new Rect(0, 0, MetaService.imageWidth2Meta, MetaService.imageHeight2Meta), 0, 0);

        // Apply the changes made to the capture texture
        capturedTexture.Apply();

        // Encode the capture texture as JPG and assign it to the CapturedImage variable
        CapturedImage = capturedTexture.EncodeToJPG();

        // Check if there is a coroutine already running and stop it
        if (coroutineControler != null)
        {
            StopCoroutine(coroutineControler);
        }

        // Start a new coroutine for the InferenceAPI using the captured image
        coroutineControler = StartCoroutine(MetaService.InferenceAPI(CapturedImage));

        // Reset the target and active render textures
        arCamera.targetTexture = null;
        RenderTexture.active = null;
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
            capturedTexture.ReadPixels(new Rect(0, 0, MetaService.imageWidth2Meta, MetaService.imageHeight2Meta), 0, 0);

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