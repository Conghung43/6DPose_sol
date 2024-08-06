using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using Vuforia;
using System;
using System.Linq;
using UnityEngine.XR.ARFoundation.Samples;
//using ZXing;
//using ZXing.QrCode;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ImgcodecsModule;

public class BarcodeInteraction : MonoBehaviour
{
    public GameObject barcodeObject;
    //BarcodeBehaviour mBarcodeBehaviour;
    //public Button nextButton;
    private string scannedBarcode;
    private string port = "5000";
    public Toggle edgeInferenceToggle;
    private string[] qrFiixData;
    [SerializeField] private TMPro.TextMeshProUGUI uiMessage;
    QRCodeDetector detector;
    /// <summary>
    /// The points.
    /// </summary>
    Mat points;

    /// <summary>
    /// The decoded info
    /// </summary>
    List<string> decodedInfo;

    /// <summary>
    /// The straight qrcode
    /// </summary>
    List<Mat> straightQrcode;

    public TrackedImageInfoManager trackedImageInfoManager;
    // public MetaAPI metaAPI;
    void Start()
    {
        detector = new QRCodeDetector();
    }

    void OnDisable()
    {
        EventManager.OnBarCodeDetectedEvent -= OnBarCodeDetectedHandler;
    }

    void OnEnable()
    {
        EventManager.OnBarCodeDetectedEvent += OnBarCodeDetectedHandler;
        points = new Mat();
        decodedInfo = new List<string>();
        straightQrcode = new List<Mat>();
    }

    void Update()
    {
        string barcodeTxt = "";
        if (StationStageIndex.FunctionIndex != "ScanBarcode")
        {
            return;
            //uiMessage.text = "Scan META";
            //StationStageIndex.FunctionIndex = "ScanBarcode";
        }

        if (edgeInferenceToggle.isOn)
        {
        //    OnBarCodeDetectedHandler();
        //}
        //else if (TrackedImageInfoManager.cpuImageTexture != null)
        //{
            try
            {
                trackedImageInfoManager.UpdateCPUImage();
                var colorByte = TrackedImageInfoManager.cpuImageTexture.GetPixels32();
                //File.WriteAllBytes("test1.jpg", TrackedImageInfoManager.cpuImageTexture.EncodeToJPG());
                //var result = barcodeReader?.Decode(colorByte, TrackedImageInfoManager.cpuImageTexture.width, TrackedImageInfoManager.cpuImageTexture.height);

                List<string> decoded_info = new List<string>();
                List<string> decoded_type = new List<string>();
                Mat corners = new Mat();
                Mat rgbaMat = new Mat(TrackedImageInfoManager.cpuImageTexture.height, TrackedImageInfoManager.cpuImageTexture.width, CvType.CV_8UC3);
                Utils.texture2DToMat(TrackedImageInfoManager.cpuImageTexture, rgbaMat);

                Imgproc.cvtColor(rgbaMat, rgbaMat, Imgproc.COLOR_RGB2BGR);

                // Save Mat to file
                string path = "SavedImage.jpg";
                Imgcodecs.imwrite(path, rgbaMat);

                bool result_detection = detector.detectAndDecodeMulti(rgbaMat,decodedInfo, points, straightQrcode);

                if (result_detection)
                {
                    barcodeTxt = decodedInfo[0];
                    uiMessage.text = barcodeTxt;
                    // Do something with the decoded QR code here
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning(ex.Message);
            }
        }

        else
        {
            return;
        }
        

        if (StationStageIndex.barcodeMetaOn) //&& StationStageIndex.barcodeFiixOn)
        {
            // barcodeObject.SetActive(false);
            edgeInferenceToggle.isOn = true;
            StationStageIndex.FunctionIndex = "VuforiaTargetDetecting";
            uiMessage.text = "Scan META success";
            //MetaApiStatic.ConnectMetaBasedProjectID(1678700647);
            return;
        }

        if (scannedBarcode != barcodeTxt)
        {
            EventManager.OnBarCodeDetectedEvent?.Invoke(this, new EventManager.OnBarCodeClickEventArgs
            {
                barcodeText = barcodeTxt
            }); ;
            //Debug.Log(mBarcodeBehaviour.transform.position);
        }
    }

    private void OnBarCodeDetectedHandler(object sender, EventManager.OnBarCodeClickEventArgs e)
    {
        string[] barcodeStringArray;
        barcodeStringArray = e.barcodeText.Replace("\'", "").Replace("\"b", "").Replace(" ", "").Replace("\\", "").Trim('[', ']').Split(new[] { ',' }).Select(x => x.Trim('"')).ToArray();//
        if (barcodeStringArray.Length > 4)//(barcodeJsonString.Contains("project_id"))
        {
            barcodeObject.SetActive(false);
            if (!StationStageIndex.barcodeMetaOn)
            {
                Inference.ip = barcodeStringArray[0];
                OnBarCodeDetectedHandler();
                //MetaService.qrMetaData = barcodeStringArray;
                //// Set Config
                //try
                //{
                //    MetaService.serverIP = MetaService.qrMetaData[0] + ":" + port;
                //    MetaService.ConnectWithMetaProjectID();
                //    //StartCoroutine(MetaApiStatic.ConnectMetaBasedProjectID_coroutine());
                //    if (MetaService.projectData.data.Count == ConfigRead.configData.DataStation[0].Datastage.Count - 1)
                //    {
                //        StationStageIndex.barcodeMetaOn = true;
                //        //nextButton.gameObject.SetActive(true);
                //    }
                //    else
                //    {
                //        uiMessage.text = "Wrong Project! " + MetaService.projectData.data.Count.ToString() + ConfigRead.configData.DataStation[0].Datastage.Count.ToString();
                //    }
                //}
                //catch
                //{
                //    uiMessage.text = "Connection error!";

                //}
                // string projectFile = $"Project/{MetaApiStatic.qrMetaData[2]}.json";
                // StartCoroutine(ConfigRead.LoadJSONFileProject(Path.Combine(Application.streamingAssetsPath,projectFile)));
            }
            barcodeObject.SetActive(true);
        }
        else //if (barcodeJsonString.Contains("accessKey"))
        {
            qrFiixData = barcodeStringArray;
            // var jsonData = JsonConvert.DeserializeObject<RSAfiixDecreptionObject>(barcodeJsonString);
            // var fixxData = 
            StationStageIndex.barcodeFiixOn = true;
            Debug.Log("Got Fiix");
            // Set config Fiix
        }
    }
    private void OnBarCodeDetectedHandler()
    {
        string[] barcodeStringArray = new string[]
                    {
                            "192.168.0.5",
                            "MTIzMTIz",
                            "compressor_vailidation",
                            "1692266345",
                            "demo"
                    };
        //"1688627566"
        if (!StationStageIndex.barcodeMetaOn)
        {
            MetaService.qrMetaData = barcodeStringArray;
            StationStageIndex.barcodeMetaOn = true;
            //nextButton.gameObject.SetActive(true);
            // Set Config
            if (ConfigRead.metaOnline)
            {
                MetaService.serverIP = MetaService.qrMetaData[0] + ":" + port;
                //MetaService.ConnectWithMetaProjectID();
            }
        }
    }
}
