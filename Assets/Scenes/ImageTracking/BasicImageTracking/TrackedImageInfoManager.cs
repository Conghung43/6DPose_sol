using System;
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

        [SerializeField]
        [Tooltip("If an image is detected but no source texture can be found, this texture is used instead.")]
        Texture2D m_DefaultTexture;
        //private Camera _mainCamera;
        public bool updateMainCamera = false;

        public static bool drawObject = true;
        public static Vector2[] bbox;

        private RenderTexture renderTexture;
        private Texture2D capturedTexture;
        bool init = false;
        bool drawCorner = false;
        private Texture2D m_CameraTexture;
        [SerializeField] private TMPro.TextMeshProUGUI logInfo;
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
            //_mainCamera = new GameObject("_mainCamera").AddComponent<Camera>();
            //_mainCamera.enabled = false;

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
            if (!init)
            {
                cameraManager.subsystem.currentConfiguration = cameraManager.GetConfigurations(Allocator.Temp)[cameraManager.GetConfigurations(Allocator.Temp).Length - 1]; //In my case 0=640*480, 1= 1280*720, 2=1920*1080
                init = true;
            }
        }

        //private void Update()
        //{
        //    if (updateMainCamera)
        //    {
        //        _mainCamera.CopyFrom(Camera.main);
        //    }
        //}

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



        void ScreenCapture(int[] tlrbBox)
        {
            Camera.main.targetTexture = renderTexture;

            // Render the AR camera
            Camera.main.Render();

            // Set the active render texture
            RenderTexture.active = renderTexture;

            // Read the pixels from the specified rectangle in the capture texture
            capturedTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);

            // Apply the changes made to the capture texture
            capturedTexture.Apply();
            Camera.main.targetTexture = null;
            RenderTexture.active = null;

            // Encode the capture texture as JPG and assign it to the CapturedImage variable
            Byte[] CapturedImage = capturedTexture.EncodeToJPG();

            StartCoroutine(JsonReader.ServerInference(CapturedImage, tlrbBox));
            //string filePath = Path.Combine(Application.persistentDataPath, count.ToString() + "_.jpg");
            // Write the byte array to a file
            //System.IO.File.WriteAllBytes(filePath, CapturedImage);
        }

        void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
        {
            foreach (var trackedImage in eventArgs.added)
            {
                // Give the initial image a reasonable default scale
                trackedImage.transform.localScale = new Vector3(0.01f, 1f, 0.01f);
                UpdateInfo(trackedImage);
                drawObject = true;
            }

            foreach (var trackedImage in eventArgs.updated)
            {
                UpdateInfo(trackedImage);
                if (drawObject)
                {
                    AddCorner(trackedImage.gameObject);
                }
                //Debug.Log(Camera.main.WorldToScreenPoint(trackedImage.transform.position));
            }
        }

        void AddCorner(GameObject trackedImage)
        {
            
            Vector3[] cornerOffsets = new Vector3[]
                {
                            new Vector3(-1f, 0, -1f),
                            new Vector3( 1f, 0, -1f),
                            //new Vector3(-1f,  0, -1f),
                            //new Vector3( 1f,  0, -1f),
                            new Vector3(-1f, 0,  1f),
                            new Vector3( 1f, 0,  1f),
                            //new Vector3(-1f,  0,  1f),
                            //new Vector3( 1f,  0,  1f)
                };
            bbox = new Vector2[4];
            Vector3 scale = trackedImage.transform.localScale;
#if UNITY_EDITOR
            scale.z = scale.z * 0.7f;
            //scale.x = scale.x * 1.1f;
#endif
            for (int i = 0; i < cornerOffsets.Length; i++)
            {

                Vector3 position = trackedImage.transform.position + trackedImage.transform.rotation * Vector3.Scale(scale * 0.5f, cornerOffsets[i]);
                if (!drawCorner)
                {
                    GameObject cornerObject = Instantiate(sphere, position, sphere.transform.rotation);
                }

                bbox[i] = Camera.main.WorldToScreenPoint(position);
                bbox[i][1] = Screen.height - bbox[i][1];
                //Debug.Log();
            }
            drawCorner = true;

            int[] tlrbBox = new int[4];
            tlrbBox[0] = Mathf.FloorToInt(bbox[0].x);
            tlrbBox[1] = Mathf.FloorToInt(bbox[0].y);
            tlrbBox[2] = Mathf.FloorToInt(bbox[0].x);
            tlrbBox[3] = Mathf.FloorToInt(bbox[0].y);

            for (int i = 1; i < bbox.Length; i++)
            {
                if (bbox[i].x < tlrbBox[0])
                {
                    tlrbBox[0] = Mathf.FloorToInt(bbox[i].x);
                }
                if (bbox[i].y < tlrbBox[1])
                {
                    tlrbBox[1] = Mathf.FloorToInt(bbox[i].y);
                }

                if (bbox[i].x > tlrbBox[2])
                {
                    tlrbBox[2] = Mathf.FloorToInt(bbox[i].x);
                }

                if (bbox[i].y > tlrbBox[3])
                {
                    tlrbBox[3] = Mathf.FloorToInt(bbox[i].y);
                }

            }

            if (tlrbBox[0] <= 0 || tlrbBox[1] <= 0 || tlrbBox[2] >= Screen.width || tlrbBox[3] >= Screen.height) return;
            trackedImage.SetActive(false);
            //ScreenCapture(tlrbBox);
            UpdateCPUImage(tlrbBox);
            drawObject = false;
        }

        void OnCameraIntrinsicsUpdated()
        {
            if (!cameraManager.TryGetIntrinsics(out XRCameraIntrinsics intrinsics))
            {
                return;
            }
            logInfo.text = intrinsics.focalLength.ToString();
            logInfo.text += intrinsics.principalPoint.ToString();
            //cameraPoses[count].sensorSize = new Vector2(arCamera.pixelWidth, arCamera.scaledPixelWidth);
        }

        unsafe void UpdateCPUImage(int[] tlrbBox)
        {

            // Attempt to get the latest camera image. If this method succeeds,
            // it acquires a native resource that must be disposed (see below).
            if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                return;
            }

            // Convert screen image points to cpu image points
            Vector2 cpuImageSize = new Vector2(image.width, image.height);
            Vector2 screenImageSize = new Vector2(Screen.width, Screen.height);
            Vector2 screenStartPointInCpuImage;
            ImageProcessing.ScreenPointToXrImagePoint(Vector2.zero,
                                        out screenStartPointInCpuImage,
                                        cpuImageSize,
                                        screenImageSize);

            for (int i = 0; i < 2; i++)
            {
                Vector2 EdgePoint = new Vector2(tlrbBox[i*2], screenImageSize.y - tlrbBox[i * 2 + 1]) / screenImageSize;
                EdgePoint = screenStartPointInCpuImage + EdgePoint * new Vector2(cpuImageSize.x, cpuImageSize.y - (screenStartPointInCpuImage.y * 2));
                tlrbBox[i * 2] = Mathf.RoundToInt(EdgePoint.x);
                tlrbBox[i * 2 + 1] = Mathf.RoundToInt(cpuImageSize.y - EdgePoint.y);
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

            byte[] CapturedImage = m_CameraTexture.EncodeToJPG();
            //System.IO.File.WriteAllBytes("test.jpg", CapturedImage);
            StartCoroutine(JsonReader.ServerInference(CapturedImage, tlrbBox));
            OnCameraIntrinsicsUpdated();
        }
    }
}