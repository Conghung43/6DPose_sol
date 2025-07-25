using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation.Samples;

public class UIController : MonoBehaviour
{
    public GameObject EnterIPAddress;
    public GameObject qrCode;
    public GameObject qrCodeFrame;
    public GameObject highlightChecklist;
    public GameObject targetDetect;
    public GameObject inferenceUI;
    public GameObject capturePage;
    public GameObject resultPage;
    public GameObject checkList;
    public GameObject ModelTarget;
    [SerializeField] private TMPro.TextMeshProUGUI overViewContent;
    public Image backgroundTopResult;
    [SerializeField] private TMPro.TextMeshProUGUI backgroundTopText;
    private GameObject[] objects;
    private bool isEventAdded;
    public GameObject flowInstruction;
    public LineRenderer lineRenderer;
    public NextStep nextStep;
    public GameObject captureBtn;

    //public DefaultObserverEventHandler observer;
    //public Vuforia.ModelTargetBehaviour targetBehaviour;
    // Start is called before the first frame update
    [SerializeField] private DescriptionController _descriptionController;

    void OnDisable()
    {
        StationStageIndex.OnFunctionIndexChange -= OnFunctionIndexChangeActionHandler;
        StationStageIndex.OnImageTargetFoundChange -= OnImageTargetFoundActionHandler;
        StationStageIndex.OnFinalUIChange -= OnFinalUIPage;
    }

    void OnEnable()
    {
        StationStageIndex.OnFunctionIndexChange += OnFunctionIndexChangeActionHandler;
        StationStageIndex.OnImageTargetFoundChange += OnImageTargetFoundActionHandler;
        StationStageIndex.OnFinalUIChange += OnFinalUIPage;
    }

    private void StartQRCodeAnimation(bool zoomIn = true)
    {
        if (!qrCode.activeSelf) return;
        if (zoomIn)
        {
            qrCodeFrame.transform.DOScale(Vector3.one * 1.1f, 0.8f).OnComplete(() => StartQRCodeAnimation(false));
        }
        else
        {
            qrCodeFrame.transform.DOScale(Vector3.one, 0.8f).OnComplete(() => StartQRCodeAnimation(true));
        }
    }

    private void OnFunctionIndexChangeActionHandler(string functionName)
    {
        Debug.LogError($"functionName {functionName}");
        _descriptionController.UpdateDescription(functionName, StationStageIndex.stageIndex);
        List<Datastage> dataStages = ConfigRead.configData.DataStation[StationStageIndex.stationIndex].Datastage;
        try
        {
            switch (functionName)
            {
                case "Home"
                    : // 2 button: Main demo or show 3d model. future approach: using vuforia area target in background
                    // uiMessage.text = "Home";
                    qrCode.SetActive(false);
                    //model3Dbtn.gameObject.SetActive(true);
                    EnterIPAddress.gameObject.SetActive(true);
                    targetDetect.SetActive(false);
                    inferenceUI.SetActive(false);
                    capturePage.SetActive(false);
                    capturePage.SetActive(false);
                    resultPage.SetActive(false);
                    lineRenderer.gameObject.SetActive(false);
                    break;
                case "ScanBarcode": // Show square bounding box
                    // uiMessage.text = "Scan META QR code";
                    EnterIPAddress.gameObject.SetActive(false);
                    qrCode.SetActive(true);
                    StartQRCodeAnimation();
                    targetDetect.SetActive(false);
                    inferenceUI.SetActive(false);
                    capturePage.SetActive(false);
                    resultPage.SetActive(false);
                    // inputField.SetActive(false);
                    break;
                case "VuforiaTargetDetecting":
                    // uiMessage.text = $"{MetaService.qrMetaData[2]} \n Target detecting..";
                    overViewContent.text = MetaService.qrMetaData[2];
                    TurnOnInferenceFlag();
                    StationStageIndex.ModelTargetFound = false;
                    //observer.enabled = true;
                    //targetBehaviour.enabled = true;
                    ModelTarget.gameObject.SetActive(false);
                    ModelTarget.gameObject.SetActive(true);
                    qrCode.SetActive(false);
                    targetDetect.SetActive(true);
                    inferenceUI.SetActive(false);
                    capturePage.SetActive(false);
                    resultPage.SetActive(false);
                    break;
                case "VuforiaTarget": // Image target: all 3D model show up
                    break;
                    //observer.enabled = true;
                    //targetBehaviour.enabled = true;
                    TurnOnInferenceFlag();
                    lineRenderer.gameObject.SetActive(false);
                    flowInstruction.SetActive(true);
                    // uiMessage.text = $"{MetaService.qrMetaData[2]} \n Target detected";
                    overViewContent.text = MetaService.qrMetaData[2];
                    targetDetect.SetActive(false);
                    if (StationStageIndex.ModelTargetFound)
                    {
                        checkList.gameObject.SetActive(true);
                        highlightChecklist.SetActive(false);
                        objects = GameObject.FindGameObjectsWithTag("Checkmark");
                        foreach (GameObject obj in objects)
                        {
                            obj.gameObject.SetActive(false);
                        }
                    }

                    break;
                case "Sample":
                    flowInstruction.SetActive(false);
                    highlightChecklist.SetActive(true);
                    inferenceUI.SetActive(true);
                    capturePage.SetActive(false);
                    resultPage.SetActive(false);
                    backgroundTopResult.gameObject.SetActive(false);
                    nextStep.ShowDetect();
                    captureBtn.SetActive(false);
                    break;
                case "Detect":
                    inferenceUI.SetActive(true);
                    capturePage.SetActive(false);
                    resultPage.SetActive(false);
                    backgroundTopResult.gameObject.SetActive(true);
                    nextStep.Activate(false);
                    captureBtn.SetActive(true);
                    break;
                case "Result":
                    targetDetect.SetActive(false);
                    inferenceUI.SetActive(false);
                    capturePage.SetActive(false);
                    resultPage.SetActive(true);
                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
        }
    }

    public void ShowInferenceResult(bool isOK)
    {
        if (isOK)
        {
            backgroundTopText.text = "OK";
            backgroundTopResult.color = new Color((float)24 / 255, (float)175 / 255, (float)121 / 255);
        }
        else
        {
            backgroundTopText.text = "NG";
            backgroundTopResult.color = new Color((float)255 / 255, (float)89 / 255, (float)89 / 255);
        }
    }

    public void OnFinalUIPage(bool finalUI)
    {
    }

    private void TurnOnInferenceFlag()
    {
        if (StationStageIndex.stageIndex == 1)
            TrackedImageInfoManager.isInferenceAvailable = true;
    }

    private void OnImageTargetFoundActionHandler(bool imageTargetFound)
    {
        if (imageTargetFound)
        {
            checkList.gameObject.SetActive(true);
            highlightChecklist.SetActive(false);
            if (StationStageIndex.FunctionIndex == "VuforiaTargetDetecting")
            {
                StationStageIndex.FunctionIndex = "VuforiaTarget";
                overViewContent.text = $"{MetaService.qrMetaData[2]} \n States Overview ";
                //objects = GameObject.FindGameObjectsWithTag("Checkmark");
                //foreach (GameObject obj in objects)
                //{
                //    obj.gameObject.SetActive(false);
                //}
            }
        }
    }
}