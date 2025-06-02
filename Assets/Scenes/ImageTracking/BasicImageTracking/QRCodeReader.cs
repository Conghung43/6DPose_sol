using UnityEngine;
using UnityEngine.UI;
using ZXing;
using UnityEngine.XR.ARFoundation.Samples;
using System.IO;

public class QRCodeReader : MonoBehaviour
{
    //public RawImage cameraDisplay;
    //private WebCamTexture camTexture;
    private IBarcodeReader barcodeReader;

    void Start()
    {
        barcodeReader = new BarcodeReader();
        //camTexture = new WebCamTexture();
        //cameraDisplay.texture = camTexture;
        //cameraDisplay.material.mainTexture = camTexture;
        //camTexture.Play();
    }

    void Update()
    {
        if (VisionOSCameraManager.Instance.GetMainCameraTexture2D() != null)
        {
            try
            {
                var colorByte = VisionOSCameraManager.Instance.GetMainCameraTexture2D().GetPixels32();
                File.WriteAllBytes("test1.jpg", VisionOSCameraManager.Instance.GetMainCameraTexture2D().EncodeToJPG());
                var result = barcodeReader.Decode(colorByte, VisionOSCameraManager.Instance.GetMainCameraTexture2D().width, VisionOSCameraManager.Instance.GetMainCameraTexture2D().height);
                if (result != null)
                {
                    Debug.Log("QR Code detected: " + result.Text);
                    // Do something with the decoded QR code here
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning(ex.Message);
            }
        }
    }
}
