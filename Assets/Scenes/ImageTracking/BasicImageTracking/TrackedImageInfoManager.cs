
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using System.IO;
using Unity.Collections;
using static UnityEngine.XR.ARSubsystems.XRCpuImage;
using System.Text.RegularExpressions;
using Unity.Collections.LowLevel.Unsafe;
using static UnityEngine.XR.ARFoundation.Samples.DynamicLibrary;
using System;



namespace UnityEngine.XR.ARFoundation.Samples
{
    /// This component listens for images detected by the <c>XRImageTrackingSubsystem</c>
    /// and overlays some information as well as the source Texture2D on top of the
    /// detected image.
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class TrackedImageInfoManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The camera to set on the world space UI canvas for each instantiated image info.")]
        Camera m_WorldSpaceCanvasCamera;

        [SerializeField]
        [Tooltip("The ARCameraManager which will produce frame events.")]
        ARCameraManager cameraManager;

        /// <summary>
        /// The prefab has a world space UI canvas,
        /// which requires a camera to function properly.
        /// </summary>
        public Camera worldSpaceCanvasCamera
        {
            get { return m_WorldSpaceCanvasCamera; }
            set { m_WorldSpaceCanvasCamera = value; }
        }

        public GameObject sphere;

        public List<GameObject> sphereList;

        [SerializeField]
        [Tooltip("If an image is detected but no source texture can be found, this texture is used instead.")]
        Texture2D m_DefaultTexture;
        //private Camera _mainCamera;
        //public bool updateMainCamera = false;

        public static bool isInferenceAvailable = true;
        public static Vector2[] bbox;

        private RenderTexture renderTexture;
        private Texture2D capturedTexture;
        bool init = false;
        //bool drawCorner = false;
        private Texture2D m_CameraTexture;
        [SerializeField] private TMPro.TextMeshProUGUI logInfo;
        private XRCameraIntrinsics intrinsics = new XRCameraIntrinsics();
        public static int[] TrackedImageCorner;
        public static Texture2D cpuImageTexture;
        public static Texture2D handTexture;
        public GameObject PoseInference;

        public GameObject box3D;
        public GameObject stickWithImageTargetObject;
        public CameraFeedToRenderTexture _CameraFeedToRenderTexture;
        public RectTransform _handRect;

        int count = 0;

        Vector3 lastCamPos = Vector3.zero;

#if UNITY_EDITOR
        XRCpuImage.Transformation m_Transformation = XRCpuImage.Transformation.None;
#else
        XRCpuImage.Transformation m_Transformation = XRCpuImage.Transformation.MirrorX;
#endif
        /// <summary>
        /// If an image is detected but no source texture can be found,
        /// this texture is used instead.
        /// </summary>
        public Texture2D defaultTexture
        {
            get { return m_DefaultTexture; }
            set { m_DefaultTexture = value; }
        }

        ARTrackedImageManager m_TrackedImageManager;

        void Awake()
        {
            //cameraManager.subsystem.currentConfiguration = cameraManager.GetConfigurations(Allocator.Temp)[cameraManager.GetConfigurations(Allocator.Temp).Length - 1];
            m_TrackedImageManager = GetComponent<ARTrackedImageManager>();
        }

        void OnEnable()
        {
            cpuImageTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);

            renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            capturedTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

            m_TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;

            cameraManager.frameReceived += OnCameraFrameReceived;
            //cameraManager.subsystem.currentConfiguration = cameraManager.GetConfigurations(Allocator.Temp)[cameraManager.GetConfigurations(Allocator.Temp).Length - 1];
        }

        private void Start()
        {
            Camera mainCamera = Camera.main;
            Matrix4x4 projectionMatrix = mainCamera.projectionMatrix;

            // Extracting intrinsic parameters
            float focalLengthX = projectionMatrix[0, 0];
            float focalLengthY = projectionMatrix[1, 1];
            float principalPointX = projectionMatrix[0, 2];
            float principalPointY = projectionMatrix[1, 2];

            Debug.Log("Focal Length X: " + focalLengthX);
            Debug.Log("Focal Length Y: " + focalLengthY);
            Debug.Log("Focal Length: " + mainCamera.focalLength.ToString());
            Debug.Log("Principal Point X: " + principalPointX);
            Debug.Log("Principal Point Y: " + principalPointY);
            
        }

        void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            handTexture = UpdateCPUImage();
            _CameraFeedToRenderTexture.UpdateTexture();
            if (!init)
            {
                cameraManager.subsystem.currentConfiguration = cameraManager.GetConfigurations(Allocator.Temp)[cameraManager.GetConfigurations(Allocator.Temp).Length - 1]; //In my case 0=640*480, 1= 1280*720, 2=1920*1080
                init = true;
            }
            
        }

        public static bool AreRectanglesIntersecting(int[] rect1, int[] rect2)
        {
            // Check if either rectangle is null or has fewer than 4 elements
            if (rect1 == null || rect2 == null || rect1.Length < 4 || rect2.Length < 4)
            {
                Debug.LogError("Invalid rectangle arrays.");
                return false;
            }

            // Extract the top, left, right, and bottom values for each rectangle
            int rect1Top = rect1[0];
            int rect1Left = rect1[1];
            int rect1Right = rect1[2];
            int rect1Bottom = rect1[3];

            int rect2Top = rect2[0];
            int rect2Left = rect2[1];
            int rect2Right = rect2[2];
            int rect2Bottom = rect2[3];

            // Check for intersection
            bool intersectingHorizontally = rect1Left < rect2Right && rect1Right > rect2Left;
            bool intersectingVertically = rect1Top < rect2Bottom && rect1Bottom > rect2Top;

            // Return true if both horizontally and vertically intersecting
            return intersectingHorizontally && intersectingVertically;
        }

        public static bool IsObjectInScreen(GameObject obj)
        {
            // Get the viewport position of the object
            Vector3 viewportPosition = Camera.main.WorldToViewportPoint(obj.transform.position);

            // Check if the viewport position is within the screen boundaries
            return viewportPosition.x >= 0 && viewportPosition.x <= 1 &&
                   viewportPosition.y >= 0 && viewportPosition.y <= 1 &&
                   viewportPosition.z > 0; // Ensure the object is in front of the camera
        }

        // Check hand center is in Engine Rect
        public bool ObjectCenterInOtherObjectRect(Vector2 objectCenter, Rect otherObjectRect)
        {
            // Check if the point is inside the rectangle
            bool isInside = otherObjectRect.Contains(objectCenter);

            if (isInside)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Convert OpenCV rect to Unity Rect
        Rect ConvertOpenCVRectToUnityRect(int[] bbox)
        {
            int left = bbox[0];
            int top = bbox[1];
            int right = bbox[2];
            int bottom = bbox[3];
            int width = right - left;
            int height = bottom - top;

            // Create a Unity Rect
            return new Rect(left, top, width, height);
        }

        private void Update()
        {
            if (isInferenceAvailable && TrackedImageCorner != null && PoseInference.activeSelf)
            {
                
                Vector2 imageSize = new Vector2(cpuImageTexture.width, cpuImageTexture.height);

                int[] bboxTrackedImage = TrackedImageCorner;
                int[] bboxMegaPose = null;
                int[] bbox = null;
                //bool isIntersecting = true;

                // Transformation

                bboxTrackedImage = ConvertBboxScreenImageToCPUimage(cpuImageTexture, bboxTrackedImage);
                // This function may return null if 2D bbox doesn't have any intersection part with screen
                bboxTrackedImage = CheckBboxPositionOnCPUImage(cpuImageTexture, bboxTrackedImage);

                // If object already settle down, consider to use object 3D box to generate 2D bbox for 6D pose inference input
                if (!Inference.objectInitialSet)
                {
                    Vector2[] megaPoseCorner = UpdateObjectTransform.GetPoints2D(box3D);
                    bboxMegaPose = GetLeftTopRightBottom(megaPoseCorner);

                    bboxMegaPose = ConvertBboxScreenImageToCPUimage(cpuImageTexture, bboxMegaPose);
                    // This function may return null if 2D bbox doesn't have any intersection part with screen
                    bboxMegaPose = CheckBboxPositionOnCPUImage(cpuImageTexture, bboxMegaPose);

                    //isIntersecting = AreRectanglesIntersecting(bboxMegaPose, bboxTrackedImage);

                    //Comparision
                    if (bboxMegaPose != null)
                    {
                        logInfo.text = "";// "bboxMegaPose" + cpuImageTexture.width.ToString() + " " + cpuImageTexture.height.ToString();

                        //if (bboxTrackedImage != null)
                        //{
                        //    if (isIntersecting)
                        //    {
                        //        bbox = bboxMegaPose;
                        //    }
                        //    else
                        //    {
                        //        count += 1;
                        //        if (count > 10)
                        //        {
                        //            count = 0;
                        //            bbox = bboxTrackedImage;
                        //        }
                        //    }
                        //}
                        bbox = bboxMegaPose;
                    }
                    else
                    {
                        if (bboxTrackedImage != null && IsObjectInScreen(stickWithImageTargetObject))
                        {
                            logInfo.text = "null,";
                            count += 1;
                            if (count > 10)
                            {
                                count = 0;
                                bbox = bboxTrackedImage;
                                Inference.objectInitialSet = true;
                            }
                        }
                        else
                        {
                            logInfo.text = "null, null";
                        }
                    }
                }
                else if (IsObjectInScreen(stickWithImageTargetObject))
                {
                    bbox = bboxTrackedImage;
                }

                //if bbox width < height => return null: in small size will return bad result
                if (bbox != null)
                {
                    // Luan If hand close to engine, skip 6D inference
                    Rect engineRect = ConvertOpenCVRectToUnityRect(bbox);
                    bool isHandInEngine = ObjectCenterInOtherObjectRect(_handRect.anchoredPosition, engineRect);
                    Debug.Log("hand inference engine"+isHandInEngine);
                    if (isHandInEngine) return;

                    //top left right bottom
                    if (!Inference.objectInitialSet)
                    {
                        float angle = Vector3.Angle(box3D.transform.forward, Camera.main.transform.position - box3D.transform.position);
                        //Debug.Log(angle.ToString());
                        if (Mathf.Abs(angle - 90) < 20)
                        {
                            bbox = null;
                        }
                    }

                    //byte[] cpuImageEncode = cpuImageTexture.EncodeToJPG();
#if !UNITY_EDITOR
                    if (intrinsics.focalLength.x == 0)
                    {
                        OnCameraIntrinsicsUpdated();
                    }
#endif
                    if (Inference.objectInitialSet)
                    {
                        float distance = Vector3.Distance(lastCamPos, Camera.main.transform.position);
                        //logInfo.text = distance.ToString();
#if UNITY_EDITOR
                        distance = 1f;
#endif

                        // Let camera move 1 cm for better inference result
                        if (distance  > 0.01f)
                        {
                            lastCamPos = Camera.main.transform.position;
                            StartCoroutine(Inference.ServerInference(cpuImageTexture, imageSize, bbox, intrinsics.focalLength, intrinsics.principalPoint));
                            isInferenceAvailable = false;
                        }
                    }
                    else
                    {
                        StartCoroutine(Inference.ServerInference(cpuImageTexture, imageSize, bbox, intrinsics.focalLength, intrinsics.principalPoint));
                        isInferenceAvailable = false;
                    }
                    logInfo.text += Inference.elMs;
                }
            }
            //return;
        }

        void OnDisable()
        {
            m_TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }

        void UpdateInfo(ARTrackedImage trackedImage)
        {
            // Set canvas camera
            var canvas = trackedImage.GetComponentInChildren<Canvas>();
            canvas.worldCamera = worldSpaceCanvasCamera;

            // Update information about the tracked image
            var text = canvas.GetComponentInChildren<Text>();
            text.text = string.Format(
                "{0}\ntrackingState: {1}\nGUID: {2}\nReference size: {3} cm\nDetected size: {4} cm",
                trackedImage.referenceImage.name,
                trackedImage.trackingState,
                trackedImage.referenceImage.guid,
                trackedImage.referenceImage.size * 100f,
                trackedImage.size * 100f);

            var planeParentGo = trackedImage.transform.GetChild(0).gameObject;
            var planeGo = planeParentGo.transform.GetChild(0).gameObject;

            // Disable the visual plane if it is not being tracked
            if (trackedImage.trackingState != TrackingState.None)
            {
                planeGo.SetActive(true);

                // The image extents is only valid when the image is being tracked
                trackedImage.transform.localScale = new Vector3(trackedImage.size.x, 1f, trackedImage.size.y);

                // Set the texture
                var material = planeGo.GetComponentInChildren<MeshRenderer>().material;
                material.mainTexture = (trackedImage.referenceImage.texture == null) ? defaultTexture : trackedImage.referenceImage.texture;
            }
            else
            {
                planeGo.SetActive(false);
            }
        }

        void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
        {
            foreach (var trackedImage in eventArgs.added)
            {
                // Give the initial image a reasonable default scale
                trackedImage.transform.localScale = new Vector3(0.01f, 1f, 0.01f);
                UpdateInfo(trackedImage);
                isInferenceAvailable = true;
            }

            foreach (var trackedImage in eventArgs.updated)
            {
                UpdateInfo(trackedImage);
                //int textLength = logInfo.text.Length;
                //if (textLength > 50)
                //{
                //    textLength = 50;
                //}
                
                if (PoseInference.activeSelf)
                {
                    //logInfo.text = trackedImage.referenceImage.name + logInfo.text;
                    TrackedImageCorner = GetTrackedImageCorner(trackedImage.gameObject);
                    stickWithImageTargetObject.transform.position = trackedImage.transform.position;
                    stickWithImageTargetObject.transform.rotation = trackedImage.transform.rotation;
                    
                }
                cpuImageTexture = handTexture;
            }
        }



        private int[] GetTrackedObjectCorner()
        {
            return null;
        }

        private int[] GetTrackedImageCorner(GameObject trackedImage)
        {
            
            Vector3[] cornerOffsets = new Vector3[]
                {
                            new Vector3(-1f, 0, -1f),
                            new Vector3( 1f, 0, -1f),
                            new Vector3(-1f, 0,  1f),
                            new Vector3( 1f, 0,  1f),
                };
            bbox = new Vector2[4];
            Vector3 scale = trackedImage.transform.localScale;
#if UNITY_EDITOR
            scale.z = scale.z * 0.7f;
#endif
            for (int i = 0; i < cornerOffsets.Length; i++)
            {

                Vector3 position = trackedImage.transform.position + trackedImage.transform.rotation * Vector3.Scale(scale * 0.5f, cornerOffsets[i]);
                //if (i == 0)
                //{
                    sphereList[i].transform.position = position;
                //}

                bbox[i] = Camera.main.WorldToScreenPoint(position);
                bbox[i][1] = Screen.height - bbox[i][1];
                //Debug.Log();
            }
            //drawCorner = true;

            int[] ltrbBox = GetLeftTopRightBottom(bbox);

            //if (tlrbBox[0] <= 0 || tlrbBox[1] <= 0 || tlrbBox[2] >= Screen.width || tlrbBox[3] >= Screen.height) return null ;

            int left = ltrbBox[0];
            int top = ltrbBox[1];
            int right = ltrbBox[2];
            int bottom = ltrbBox[3];

            // Check if the bounding box is entirely outside the image boundaries
            if (left >= Screen.width || right <= 0 || top >= Screen.height || bottom <= 0)
            {
                return null;
            }





            // Adjust the bounding box if any part is outside the image boundaries
            if (left < 0) left = 0;
            if (top < 0) top = 0;
            if (right > Screen.width) right = Screen.width;
            if (bottom > Screen.height) bottom = Screen.height;



            //trackedImage.SetActive(false);
            return new int[] { left, top, right, bottom };
        }

        int[] GetLeftTopRightBottom(Vector2[] bbox)
        {
            int[] ltrbBox = new int[4];
            ltrbBox[0] = Mathf.FloorToInt(bbox[0].x);
            ltrbBox[1] = Mathf.FloorToInt(bbox[0].y);
            ltrbBox[2] = Mathf.FloorToInt(bbox[0].x);
            ltrbBox[3] = Mathf.FloorToInt(bbox[0].y);

            for (int i = 1; i < bbox.Length; i++)
            {
                if (bbox[i].x < ltrbBox[0])
                {
                    ltrbBox[0] = Mathf.FloorToInt(bbox[i].x);
                }
                if (bbox[i].y < ltrbBox[1])
                {
                    ltrbBox[1] = Mathf.FloorToInt(bbox[i].y);
                }

                if (bbox[i].x > ltrbBox[2])
                {
                    ltrbBox[2] = Mathf.FloorToInt(bbox[i].x);
                }

                if (bbox[i].y > ltrbBox[3])
                {
                    ltrbBox[3] = Mathf.FloorToInt(bbox[i].y);
                }

            }
            return ltrbBox;
        }

        void OnCameraIntrinsicsUpdated()
        {
            if (!cameraManager.TryGetIntrinsics(out intrinsics))
            {
                return;
            }
            //cameraPoses[count].sensorSize = new Vector2(arCamera.pixelWidth, arCamera.scaledPixelWidth);
        }
        //private int count = 0;
        public unsafe Texture2D UpdateCPUImage()
        {

            // Attempt to get the latest camera image. If this method succeeds,
            // it acquires a native resource that must be disposed (see below).
            if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                return null;
            }

            // Choose an RGBA format.
            // See XRCpuImage.FormatSupported for a complete list of supported formats.
            var format = TextureFormat.RGBA32;
            //logMessage.text = image.width.ToString();
            // Convert the image to format, flipping the image across the Y axis.
            // We can also get a sub rectangle, but we'll get the full image here.
            var conversionParams = new XRCpuImage.ConversionParams(image, format, m_Transformation);

            if (m_CameraTexture == null || m_CameraTexture.width != image.width || m_CameraTexture.height != image.height)
            {
                m_CameraTexture = new Texture2D(image.width, image.height, format, false);
            }

            // Texture2D allows us write directly to the raw texture data
            // This allows us to do the conversion in-place without making any copies.
            var rawTextureData = m_CameraTexture.GetRawTextureData<byte>();
            try
            {
                image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
            }
            finally
            {
                // We must dispose of the XRCpuImage after we're finished
                // with it to avoid leaking native resources.
                image.Dispose();
            }

            // Apply the updated texture data to our texture
            m_CameraTexture.Apply();

            // Save image
            //byte[] jpgImage = m_CameraTexture.EncodeToJPG();
            //float timeInSeconds = Time.time;
            //float timeInMilliseconds = timeInSeconds * 1000;
            //File.WriteAllBytes($"Images/{timeInMilliseconds}.jpg", jpgImage);

            return m_CameraTexture;
        }

        private int[] ConvertBboxScreenImageToCPUimage(Texture2D cpuTexture, int[] ltrbBox)
        {
            // Convert screen image points to cpu image points
            Vector2 cpuImageSize = new Vector2(cpuTexture.width, cpuTexture.height);
            Vector2 screenImageSize = new Vector2(Screen.width, Screen.height);
            Vector2 screenStartPointInCpuImage;
            ImageProcessing.ScreenPointToXrImagePoint(Vector2.zero,
                                        out screenStartPointInCpuImage,
                                        cpuImageSize,
                                        screenImageSize);

            for (int i = 0; i < 2; i++)
            {
                Vector2 EdgePoint = new Vector2(ltrbBox[i * 2], screenImageSize.y - ltrbBox[i * 2 + 1]) / screenImageSize;
                EdgePoint = screenStartPointInCpuImage + EdgePoint * new Vector2(cpuImageSize.x, cpuImageSize.y - (screenStartPointInCpuImage.y * 2));
                ltrbBox[i * 2] = Mathf.RoundToInt(EdgePoint.x);
                ltrbBox[i * 2 + 1] = Mathf.RoundToInt(cpuImageSize.y - EdgePoint.y);
            }

            return ltrbBox;
        }

        private int[] CheckBboxPositionOnCPUImage(Texture2D cpuTexture, int[] ltrbBox)
        {
            int left = ltrbBox[0];
            int top = ltrbBox[1];
            int right = ltrbBox[2];
            int bottom = ltrbBox[3];
            int width = cpuTexture.width;
            int height = cpuTexture.height;

            // Check if the bounding box is entirely outside the image boundaries
            if (left >= width || right <= 0 || top >= height || bottom <= 0)
            {
                return null;
            }

            // Adjust the bounding box if any part is outside the image boundaries
            if (left < 0) left = 0;
            if (top < 0) top = 0;
            if (right > width) right = width;
            if (bottom > height) bottom = height;

            return new int[] { left, top, right, bottom};
        }


    }
}