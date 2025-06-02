using System.Collections;
using AOT;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Text;
using UnityEngine.UI;

public class VisionOSCameraManager:MonoBehaviour
{
#if UNITY_VISIONOS
    public RawImage Image;
    private static VisionOSCameraManager instance;

    public static VisionOSCameraManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<VisionOSCameraManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(VisionOSCameraManager).Name;
                    instance = obj.AddComponent<VisionOSCameraManager>();
                }
            }
            return instance;
        }
    }
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as VisionOSCameraManager;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Destroy(gameObject);
        }
    }
    
     private Texture2D tmpTexture = null;
     // private Texture2D resizedTexture = null;
     private IntPtr _texturePointer;
     private IntPtr _intrinsicsPointer;
     private IntPtr _extrinsicsPointer;
     private string tempBase64String = "";
     private float skipSeconds = 0.1f;
     private bool isStart = false;
     private bool isPermission = false;
     public int originalWidth = 1920;
     public int originalHeight = 1080;
     private int _targetWidth = 1280;
     private int _targetHeight = 720;
     public float[] intrinsicsData = new float[9];  // 3x3 matrix for intrinsics
     public float[] extrinsicsData = new float[16]; // 4x4 matrix for extrinsics

     private void OnEnable()
     {
#if !UNITY_VISIONOS || UNITY_EDITOR
         gameObject.SetActive(false);
         return;
#endif
         if (isPermission) return;
         AskPermission();
         isPermission = true;
     }

     private void Start()
     {
#if !UNITY_VISIONOS || UNITY_EDITOR
         return;
#endif
         tmpTexture = new Texture2D(originalWidth, originalHeight, TextureFormat.BGRA32, false);
         // resizedTexture = new Texture2D(_targetWidth, _targetHeight, TextureFormat.BGRA32, false);
         StartCamera();
     }

     private void Update()
     {
#if !UNITY_VISIONOS || UNITY_EDITOR
         return;
#endif
         UpdateCamera();
     }

     /// <summary>
     /// Get Vision Pro main camera image as texture2D.
     /// </summary>
     /// <returns></returns>
     public Texture2D GetMainCameraTexture2D()
     {
         return tmpTexture;
     }

     public void AskPermission()
     {
         askCameraPermission();
     }

     public void StartCamera()
     {
         if (isStart) return;
         isStart = true;
         startVisionProMainCamera();
     }

     public void UpdateCamera()
     {
         if (!isStart) return;
         if (_texturePointer != IntPtr.Zero)
         {
             UpdateTexture(_texturePointer);
         }
         else
         {
             _texturePointer = getTexturePointer();
         }

         if (_intrinsicsPointer == IntPtr.Zero)
         {
             _intrinsicsPointer = getIntrinsicsPointer();
         }
         else
         {
             UpdateIntrinsics(_intrinsicsPointer);
         }

         if (_extrinsicsPointer == IntPtr.Zero)
         {
             _extrinsicsPointer = getExtrinsicsPointer();
         }
         else
         {
             UpdateExtrinsics(_extrinsicsPointer);
         }
     }

     public void StopCamera()
     {
         if (!isStart) return;
         isStart = false;
         stopVisionProMainCamera();
     }

     public bool IsPlaying()
     {
         return isStart && tmpTexture.width > 0;
     }

     private void UpdateTexture(IntPtr dataPointer)
     {
         int bufferSize = originalWidth * originalHeight * 4; // BGRA per pixel has 4 bytes

         // copy pointer data
         byte[] rawData = new byte[bufferSize];
         Marshal.Copy(dataPointer, rawData, 0, bufferSize);

         tmpTexture.LoadRawTextureData(rawData);
         tmpTexture.Apply();
         Image.texture = tmpTexture;
         // ResizeTexture(tmpTexture);
     }
     
     private void UpdateIntrinsics(IntPtr dataPointer)
     {
         Marshal.Copy(dataPointer, intrinsicsData, 0, intrinsicsData.Length);
     }

     private void UpdateExtrinsics(IntPtr dataPointer)
     {
         Marshal.Copy(dataPointer, extrinsicsData, 0, extrinsicsData.Length);
     }
     
     private void ResizeTexture(Texture2D sourceTexture)
     {
         // RenderTexture rt = RenderTexture.GetTemporary(_targetWidth, _targetHeight);
         // RenderTexture.active = rt;
         // Graphics.Blit(sourceTexture, rt);
         // resizedTexture.ReadPixels(new Rect(0, 0, resizedTexture.width, resizedTexture.height), 0, 0);
         // resizedTexture.Apply();
         // RenderTexture.ReleaseTemporary(rt);
         // RenderTexture.active = null;
     }

     [DllImport("__Internal")]
     static extern void startVisionProMainCamera();

     [DllImport("__Internal")]
     static extern void stopVisionProMainCamera();

     [DllImport("__Internal")]
     static extern void askCameraPermission();

     [DllImport("__Internal")]
     private static extern IntPtr getTexturePointer();

     [DllImport("__Internal")]
     private static extern IntPtr getIntrinsicsPointer();

     [DllImport("__Internal")]
     private static extern IntPtr getExtrinsicsPointer();
#endif
}