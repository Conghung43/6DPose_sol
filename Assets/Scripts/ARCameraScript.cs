using System;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using Rect = UnityEngine.Rect;

public class ARCameraScript : MonoBehaviour
{
    private static ARCameraScript instance;

    public static ARCameraScript Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ARCameraScript>();
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(ARCameraScript).Name;
                    instance = obj.AddComponent<ARCameraScript>();
                }
            }

            return instance;
        }
    }

    [SerializeField] private Texture2D greenBBoxTexture;
    [SerializeField] private Texture2D redBBoxTexture;
    [SerializeField] private Sprite greenBBox;
    [SerializeField] private Sprite redBBox;
    [SerializeField] private Sprite grayBBox;
    [SerializeField] private Camera arCamera;
    [SerializeField] private int bBoxBorderSize = -1;
    [SerializeField] private TMPro.TextMeshProUGUI titleInfo;

    public GameObject sphere;

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
    public Texture2D resizeTextureOnnx;
    private RenderTexture resizeRenderTextureOnnx;
    public bool inferenceResponseFlag = true;

    public ComputeShader normalizeShader;
    private int width = 224;
    private int height = 224;
    private float[] mean = { 0.485f, 0.456f, 0.406f };
    private float[] std = { 0.229f, 0.224f, 0.225f };
    public int inferenceClass = 0;
    public int lastInferenceClass = 0;
    private List<int> smoothInference = Enumerable.Repeat(0, 20).ToList();
    public float[] ImageFloatValues;
    public float[] prb;

    public GameObject[] checkingResult;
    public Material checkingObjectMaterial;

    int count = 0;

    public EdgeInferenceBarracuda edgeInference;

    //Stopwatch inferenceWatch = new Stopwatch();
    public JsonDeserialization.InferenceResult metaAPIinferenceData;

    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private float savedFieldOfView;

    bool isInferenceFromSample = false;
    int count_inference = 0;
    int count_detect_mode = 0;

    public NextStep nextStep;
    public Rect _dectionRect;

    public Transform body;

    private void Start()
    {
        // Set the StationStageIndex FunctionIndex to "Home"
        StationStageIndex.FunctionIndex = "Home";

        // Subscribe to the OnInferenceResponse event in MetaApiStatic
        MetaService.OnInferenceResponse += OnInferenceResponse;

        EventManager.OnStageChange += ResetBoundingBox;

        // Create a new RenderTexture with specified dimensions
        renderTexture = new RenderTexture(MetaService.imageWidth2Meta, MetaService.imageHeight2Meta, 24);
        screenRenderTexture = new RenderTexture(VisionOSCameraManager.Instance.originalWidth,
            VisionOSCameraManager.Instance.originalHeight, 24);

        // Create a new Texture2D with specified dimensions and format
        capturedTexture = new Texture2D(MetaService.imageWidth2Meta, MetaService.imageHeight2Meta, TextureFormat.RGB24,
            false);
        screenTexture = new Texture2D(VisionOSCameraManager.Instance.originalWidth,
            VisionOSCameraManager.Instance.originalHeight, TextureFormat.RGB24, false);

        //network = new NNetwork(modelAsset);
        EventManager.OnCheckpointUpdateEvent += Inference;

        resizeTextureOnnx = new Texture2D(width, height);
        resizeRenderTextureOnnx = new RenderTexture(width, height, 24);

        // Set the border of the GUI style with a specified size
        guiStyle.border = new RectOffset(bBoxBorderSize, bBoxBorderSize, bBoxBorderSize, bBoxBorderSize);
    }

    private void ResetBoundingBox(object sender, EventManager.OnStageIndexEventArgs e)
    {
        // Reset bbox when stage change
        // _detectImage.sprite = grayBBox;
        smoothInference = Enumerable.Repeat(0, 20).ToList();
    }

    private void OnInferenceResponse(object sender, MetaService.OnInferenceResponseEventArgs e)
    {
        // Listen event from Inference response in Meta API, event parameter will be drawn in this function
        // UnityEngine.Debug.Log("START ARCameraScript/OnInferenceResponse ");

        // Check if the function index is not "Detect"
        //if (StationStageIndex.FunctionIndex != "Detect")
        //{
        //    inferenceResponseFlag = true;
        //    return;
        //}

        try
        {
            // Deserialize the inference response
            metaAPIinferenceData =
                JsonConvert.DeserializeObject<JsonDeserialization.InferenceResult>(e.inferenceResponse);

            // Check if rule data is not null
            if (!metaAPIinferenceData.data.rule.Equals(null))
            {
                if (StationStageIndex.FunctionIndex == "Sample")
                {
                    Vector3 centerPoint;
                    float radiusOnScreen;
                    Vector3 centerPoint3D;
                    //Base class id
                    //List<int> indices = FindIndicesOfValue(metaAPIinferenceData.data.class_ids,
                    //    StationStageIndex.stageIndex - 1);
                    //base clase name
                    int classid = FindClassIDfromName((StationStageIndex.stageIndex).ToString());
                    List<int> indices = FindIndicesOfValue(metaAPIinferenceData.data.class_ids,
                        classid);
                    int bestScoreIndex = FindBestScoreIndex(indices, metaAPIinferenceData.data.scores);

                    if (bestScoreIndex >= 0)
                    {
                        (centerPoint, radiusOnScreen, centerPoint3D) = GetObjectCenterRadiusBaseAI(bestScoreIndex);
                        if (centerPoint != Vector3.zero)
                        {
                            sphere.transform.position = centerPoint3D;
                            sphere.gameObject.SetActive(true);
                            //Vector2 screenPoint = arCamera.WorldToScreenPoint(sphere.transform.position);
                            // _imageDection.gameObject.SetActive(true);
                            // _imageDection.transform.position = centerPoint;
                            _dectionRect = new Rect(centerPoint.x - w / 2, centerPoint.y - h / 2, w, h);
                        }
                    }

                    // nextStep.CallAutoNextAfterDelay(5);
                }
                else
                {
                    if (count_detect_mode == 0)
                    {
                        metaAPIinferenceData = null;
                    }

                    count_detect_mode += 1;
                    sphere.gameObject.SetActive(false);
                }
                //if (StationStageIndex.FunctionIndex == "Detect") {
                //    DrawRois(false);
                //}
                //else if (StationStageIndex.FunctionIndex == "Result") {
                //    DrawRois(true);
                //}
                //else
                //{
                //    _detectImage.gameObject.SetActive(false);
                //}

                // Set Detection result
                if (!StationStageIndex.metaInferenceRule &&
                    metaAPIinferenceData.data.rule &&
                    StationStageIndex.FunctionIndex == "Detect" &&
                    !isInferenceFromSample)
                {
                    if (count_inference >= 3)
                    {
                        StationStageIndex.metaInferenceRule = metaAPIinferenceData.data.rule;
                        dataStages = ConfigRead.configData.DataStation[StationStageIndex.stationIndex].Datastage;

                        // Show/hide next step and capture buttons based on current stage
                        if (StationStageIndex.stageIndex < dataStages.Count)
                        {
                            if (StationStageIndex.stageIndex != 4)
                            {
                                nextStep.CallAutoNextAfterDelay(2);
                            }
                        }

                        // Stop the metaTimeCount if it's not null
                        if (StationStageIndex.metaTimeCount != null)
                        {
                            StationStageIndex.metaTimeCount.Stop();
                        }

                        // Find the checkListGameObject and show the checkmark
                        checkListGameObject = GameObject.Find("CP" + StationStageIndex.stageIndex.ToString());
                        checkMarkTransform =
                            checkListGameObject.transform.Find("Background").transform.Find("Checkmark");
                        checkMarkTransform.gameObject.SetActive(true);
                        count_inference = 0;
                        count_detect_mode = 0;
                    }

                    count_inference += 1;
                }
            }
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR
            // UnityEngine.Debug.LogError("ERROR ARCameraScript/OnInferenceResponse" + ex.ToString());
#endif
        }

        // Clean up and set inferenceResponseFlag to true
        e = null;
        inferenceResponseFlag = true;

        // UnityEngine.Debug.Log("END ARCameraScript/OnInferenceResponse ");
    }

    private int FindClassIDfromName(string name)
    {
        List<JsonDeserialization.Class> classList = MetaService.projectData.data[0].model.class_name;

        for (int i = 0; i < classList.Count; i++)
        {
            if (classList[i].name == name)
            {
                return i;
            }
        }


        return -1;
    }

    private List<int> FindIndicesOfValue(int[] array, int value)
    {
        List<int> indices = new List<int>();

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == value)
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    private int FindBestScoreIndex(List<int> listIndices, float[] scores)
    {
        if (listIndices == null || listIndices.Count == 0)
        {
            // Debug.LogWarning("List of indices is empty or null.");
            return -1; // Indicating an invalid index
        }

        int bestIndex = listIndices[0];
        float bestScore = scores[bestIndex];

        foreach (int index in listIndices)
        {
            if (scores[index] > bestScore)
            {
                bestScore = scores[index];
                bestIndex = index;
            }
        }

        return bestIndex;
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

    private float w, h;

    public (Vector3, float, Vector3) GetObjectCenterRadiusBaseAI(int bestScoreIndex)
    {
        int x1 = 0, y1, x2 = 0, y2;
        Vector2 currenPosition = Vector2.zero;
        //bestScoreIndex = metaAPIinferenceData.data.rois.Count - bestScoreIndex;

        if (metaAPIinferenceData != null && metaAPIinferenceData.data.rois.Count > 0)
        {
            x1 = metaAPIinferenceData.data.rois[bestScoreIndex][0];
            y1 = metaAPIinferenceData.data.rois[bestScoreIndex][1];
            x2 = metaAPIinferenceData.data.rois[bestScoreIndex][2];
            y2 = metaAPIinferenceData.data.rois[bestScoreIndex][3];
            currenPosition = new Vector2((x1 + x2) / 2, (y1 + y2) / 2);

            w = Mathf.Abs(x1 - x2);
            h = Mathf.Abs(y2 - y1);

            Vector2 CheckedPoint = PositionOptimizer2D.UpdatePosition(currenPosition, Math.Abs(x2 - x1));
            Vector2 centerPoint2D = CheckedPoint;
            //Convert cpuImage point to ScreenPoint
            Vector2 screenPoint = Vector2.zero;
            Vector2 xrImageSize = new Vector2(VisionOSCameraManager.Instance.originalWidth,
                VisionOSCameraManager.Instance.originalHeight);
            Vector2 ScreenImageSize = new Vector2(VisionOSCameraManager.Instance.originalWidth,
                VisionOSCameraManager.Instance.originalHeight);
            ImageProcessing.XrImagePointToScreenPoint(centerPoint2D, out screenPoint, xrImageSize, ScreenImageSize);

            centerPoint2D = screenPoint;
            Vector3 visionOSCameraPos = new Vector3(arCamera.transform.position.x, arCamera.transform.position.y,
                arCamera.transform.position.z - 800);

            float depth = Vector3.Distance(visionOSCameraPos, body.position);

            Vector3 centerPoint3D = Vector3.zero;
            // For optimize
            if (StationStageIndex.FunctionIndex == "Sample")
            {
                centerPoint3D = arCamera.ScreenToWorldPoint(new Vector3(
                    (Mathf.Clamp(screenPoint.x, 520f, 1920f) - 520f) / 1400f * 1920f,
                    VisionOSCameraManager.Instance.originalHeight - screenPoint.y, depth));
                centerPoint3D = new Vector3(centerPoint3D.x, centerPoint3D.y, centerPoint3D.z - 800);
            }

            float radiusOnScreen = (x2 - x1) * VisionOSCameraManager.Instance.originalWidth / (2 * xrImageSize.x);
            if (CheckedPoint == Vector2.zero)
            {
                centerPoint2D = Vector2.zero;
                centerPoint3D = Vector3.zero;
            }

            if (centerPoint2D != Vector2.zero)
            {
                centerPoint2D = new Vector2(centerPoint2D.x,
                    VisionOSCameraManager.Instance.originalHeight - centerPoint2D.y);
            }

            return (centerPoint2D, radiusOnScreen, centerPoint3D);
        }
        else
        {
            return (Vector3.zero, 1f, Vector3.zero);
        }
    }

    public UnityEngine.Rect GetObjectBBox(int bestScoreIndex)
    {
        Vector2 centerPoint;
        float radiusOnScreen;
        Vector3 centerPoint3D;
        (centerPoint, radiusOnScreen, centerPoint3D) = GetObjectCenterRadiusBaseAI(bestScoreIndex);
        if (centerPoint == Vector2.zero)
        {
            return Rect.zero;
        }

        Vector3 corner1World2D = (Vector3)centerPoint - new Vector3(radiusOnScreen, radiusOnScreen, 0f);
        UnityEngine.Rect unityRect = new UnityEngine.Rect((int)(corner1World2D[0]), (int)(corner1World2D[1]),
            radiusOnScreen * 2, radiusOnScreen * 2);
        return unityRect;
    }

    public void DrawRois(bool drawOnResultStage)
    {
        for (int i = 0; i < checkingResult.Length; i++)
        {
            checkingResult[i].SetActive(i == StationStageIndex.stageIndex - 1);
        }

        bool inferenceStatus = false;
        List<int> indices = FindIndicesOfValue(metaAPIinferenceData.data.class_ids,
            (StationStageIndex.stageIndex - 1) * 2 + 1);
        if (indices != null && indices.Count == 0)
        {
            indices = FindIndicesOfValue(metaAPIinferenceData.data.class_ids,
                (StationStageIndex.stageIndex - 1) * 2);
            if (indices != null && indices.Count > 0) inferenceStatus = true;
        }

        int bestScoreIndex = FindBestScoreIndex(indices, metaAPIinferenceData.data.scores);

        if (bestScoreIndex >= 0 && !drawOnResultStage)
        {
            Rect tempBBoxRect = GetObjectBBox(bestScoreIndex);
            if (tempBBoxRect != Rect.zero) bBoxRect = tempBBoxRect;
        }
        else
        {
            inferenceStatus = false;
            // _detectImage.gameObject.SetActive(false);
        }

        //_detectImage.gameObject.SetActive(true);

        // Set GUI style and label based on meta inference rule
        if (inferenceStatus)
        {
            checkingObjectMaterial.color = Color.green;
            // _detectImage.sprite = greenBBox;
            guiStyle.normal.background = greenBBoxTexture;
        }
        else
        {
            checkingObjectMaterial.color = Color.red;
            // _detectImage.sprite = redBBox;
            guiStyle.normal.background = redBBoxTexture;
        }
    }

    void UpdateIfClassChange(Sprite bboxTexture, bool isPass)
    {
        checkListGameObject = GameObject.Find("CP" + StationStageIndex.stageIndex.ToString());
        checkMarkTransform = checkListGameObject.transform.Find("Background").transform.Find("Checkmark");
        checkMarkTransform.gameObject.SetActive(isPass);
    }

    public void
        DrawGUI() // this function is computational => using prefab instead to show 2D bbox when inference returned
    {
        if (StationStageIndex.FunctionIndex == "Detect")
        {
            DrawRois(false);
        }
        else if (StationStageIndex.FunctionIndex == "Result")
        {
            DrawRois(true);
        }
        else
        {
            foreach (var resultObject in checkingResult)
            {
                resultObject.SetActive(false);
            }
        }

        if (StationStageIndex.FunctionIndex == "Sample")
        {
            smoothInference = Enumerable.Repeat(0, 20).ToList();
        }
    }

    private void Update()
    {
        if (sphere.gameObject.activeInHierarchy)
        {
            Vector2 screenPoint = arCamera.WorldToScreenPoint(sphere.transform.position);
            _dectionRect = new Rect(screenPoint.x - w / 2, screenPoint.y - h / 2, w, h);
        }
        else
        {
            // _detectImage.gameObject.SetActive(false);
        }

        // Check if the current function index is "Detect"
        if ((StationStageIndex.FunctionIndex == "Detect" || StationStageIndex.FunctionIndex == "Sample") &&
            !EdgeInferenceBarracuda.isSpeedRealCameraARFast(.05f))
        {
            // Ignore object out of view
            //Vector3 screenPoint = arCamera.WorldToViewportPoint(StationStageIndex.stagePosition);
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
            //titleInfo.text = $"{StationStageIndex.metaTimeCount.Elapsed.Minutes}:{StationStageIndex.metaTimeCount.Elapsed.Seconds}";// +  " " + usedMemory / 1024000 + " MB";
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
        // UnityEngine.Debug.Log("Update start:");
        // Edge inference
        if (inferenceResponseFlag)
        {
            // UnityEngine.Debug.Log("else if (inferenceResponseFlag)");
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

            if (StationStageIndex.FunctionIndex == "Detect" || StationStageIndex.FunctionIndex == "Sample")
            {
                if (StationStageIndex.FunctionIndex == "Sample")
                {
                    isInferenceFromSample = true;
                }
                else
                {
                    isInferenceFromSample = false;
                }

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
        float bboxX = bBoxRect.x; // + bBoxRect.width/2);
#if UNITY_EDITOR
        float bboxY = (float)(VisionOSCameraManager.Instance.originalHeight - bBoxRect.y - bBoxRect.height);
#else
        float bboxY = (float)(VisionOSCameraManager.Instance.originalHeight- bBoxRect.y- bBoxRect.height);
#endif
        float bboxW = (float)bBoxRect.width;
        float bboxH = (float)bBoxRect.height;
        if (bboxW <= 0 || bboxH <= 0 || bboxX <= 0 || bboxX + bboxW >= VisionOSCameraManager.Instance.originalWidth ||
            bboxY <= 0 ||
            bboxY + bboxH >= VisionOSCameraManager.Instance.originalHeight)
        {
            //logInfo.text = "Return" + bboxW.ToString() + " " + bboxH.ToString() + " " + bboxX.ToString() + " " + bboxY.ToString();
            inferenceResponseFlag = true;
            return;
        }

        try
        {
            // Crop image from cpu image
            if (VisionOSCameraManager.Instance.GetMainCameraTexture2D() != null)
            {
                int cpuWidth = VisionOSCameraManager.Instance.originalWidth;
                int cpuHeight = VisionOSCameraManager.Instance.originalHeight;
                Vector2 cpuImageSize = new Vector2(cpuWidth, cpuHeight);
                Vector2 screenImageSize = new Vector2(cpuWidth, cpuHeight);
                Vector2 screenStartPointInCpuImage;
                ImageProcessing.ScreenPointToXrImagePoint(Vector2.zero,
                    out screenStartPointInCpuImage,
                    cpuImageSize,
                    screenImageSize);
                UnityEngine.Rect newRect = new UnityEngine.Rect((int)((bboxX / cpuWidth) * cpuWidth),
                    (int)((bboxY / cpuHeight) * (cpuWidth * cpuHeight / cpuWidth) +
                          screenStartPointInCpuImage.y),
                    (int)(bboxW * cpuWidth / cpuWidth),
                    (int)(bboxW * cpuWidth / cpuWidth));
                Texture2D croppedTexture = new Texture2D((int)newRect.width, (int)newRect.height);
                croppedTexture = ImageProcessing.CropTexture2D(VisionOSCameraManager.Instance.GetMainCameraTexture2D(),
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
            UnityEngine.Debug.LogError(ex.Message);
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
        normalizeShader.SetVector("TextureDimensions",
            new Vector2(imageTexture.width, imageTexture.height)); // Set texture dimensions

        uint maxThreadGroupSizeX, maxThreadGroupSizeY, maxThreadGroupSizeZ;
        normalizeShader.GetKernelThreadGroupSizes(kernelIndex, out maxThreadGroupSizeX, out maxThreadGroupSizeY,
            out maxThreadGroupSizeZ);

        int threadGroupsX =
            (int)Mathf.CeilToInt(imageTexture.width / maxThreadGroupSizeX); // Adjust thread group size as needed
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
        if (VisionOSCameraManager.Instance.GetMainCameraTexture2D() == null)
        {
            inferenceResponseFlag = true;
            return;
        }

        CapturedImage =
            VisionOSCameraManager.Instance.GetMainCameraTexture2D().EncodeToJPG(); //capturedTexture.EncodeToJPG();

        //Also get depth image for convert 2D to 3D
        //PointCloudTracking.uploadDepthImage = true;

        //Get current camera pose
        SaveCameraState();

//#if UNITY_EDITOR
//        File.WriteAllBytes("meta.jpg", CapturedImage);
//#else
//        string filePath = Path.Combine(Application.persistentDataPath, count.ToString() + ".jpg");
//        count += 1;
//        File.WriteAllBytes(filePath, CapturedImage);
//#endif
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
    }

    private Vector3 GetScreenSpacePoint(Vector3 worldPosition)
    {
        // Convert the world position to screen space point
        Vector3 screenSpacePoint = arCamera.WorldToScreenPoint(worldPosition);

        // Invert the y-coordinate to match the screen space (0,0) at the bottom left
        screenSpacePoint.y = VisionOSCameraManager.Instance.originalHeight - screenSpacePoint.y;

        return screenSpacePoint;
    }
}