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
        if (TrackedImageInfoManager.cpuImageTexture != null)
        {
            try
            {
                var colorByte = TrackedImageInfoManager.cpuImageTexture.GetPixels32();
                File.WriteAllBytes("test1.jpg", TrackedImageInfoManager.cpuImageTexture.EncodeToJPG());
                var result = barcodeReader.Decode(colorByte, TrackedImageInfoManager.cpuImageTexture.width, TrackedImageInfoManager.cpuImageTexture.height);
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
