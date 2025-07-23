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
    public GameObject checkList;
    public GameObject ModelTarget;
    [SerializeField] private TMPro.TextMeshProUGUI uiMessage;
    public Image backgroundTopResult;
    [SerializeField] private TMPro.TextMeshProUGUI backgroundTopText;
    private GameObject[] objects;
    public Texture2D okImage;
    public Texture2D ngImage;
    private bool isEventAdded;
    public GameObject flowInstruction;
    public LineRenderer lineRenderer;

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
        _descriptionController.UpdateDescription(functionName, StationStageIndex.stageIndex);
        List<Datastage> dataStages = ConfigRead.configData.DataStation[StationStageIndex.stationIndex].Datastage;
        try
        {
            switch (functionName)
            {
                case "Home"
                    : // 2 button: Main demo or show 3d model. future approach: using vuforia area target in background
                    uiMessage.text = "Home";
                    qrCode.SetActive(false);
                    //model3Dbtn.gameObject.SetActive(true);
                    EnterIPAddress.gameObject.SetActive(true);
                    targetDetect.SetActive(false);
                    inferenceUI.SetActive(false);
                    ModelTarget.SetActive(false);
                    DisableMainUI();
                    lineRenderer.gameObject.SetActive(false);
                    break;
                case "ScanBarcode": // Show square bounding box
                    uiMessage.text = "Scan META QR code";
                    EnterIPAddress.gameObject.SetActive(false);
                    qrCode.SetActive(true);
                    StartQRCodeAnimation();
                    targetDetect.SetActive(false);
                    inferenceUI.SetActive(false);
                    // inputField.SetActive(false);
                    DisableMainUI();
                    break;
                case "VuforiaTargetDetecting":
                    uiMessage.text = $"{MetaService.qrMetaData[2]} \n Target detecting..";
                    TurnOnInferenceFlag();
                    StationStageIndex.ModelTargetFound = false;
                    //observer.enabled = true;
                    //targetBehaviour.enabled = true;
                    ModelTarget.gameObject.SetActive(false);
                    ModelTarget.gameObject.SetActive(true);
                    DisableMainUI();
                    qrCode.SetActive(false);
                    targetDetect.SetActive(true);
                    inferenceUI.SetActive(false);
                    break;
                case "VuforiaTarget": // Image target: all 3D model show up
                    break;
                    //observer.enabled = true;
                    //targetBehaviour.enabled = true;
                    TurnOnInferenceFlag();
                    lineRenderer.gameObject.SetActive(false);
                    flowInstruction.SetActive(true);
                    uiMessage.text = $"{MetaService.qrMetaData[2]} \n Target detected";
                    targetDetect.SetActive(false);
                    if (StationStageIndex.ModelTargetFound)
                    {
                        checkList.gameObject.SetActive(true);
                        highlightChecklist.SetActive(false);
                        uiMessage.text = $"{MetaService.qrMetaData[2]} \n Checkpoint Overview";
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

                   
                    uiMessage.text = $"Instruction {StationStageIndex.stageIndex}/{dataStages.Count - 1}";
                    break;
                case "Detect":
                    lineRenderer.gameObject.SetActive(false);
                    uiMessage.text = $"META AIVI {StationStageIndex.stageIndex}/{dataStages.Count - 1}";
                    
                    inferenceUI.SetActive(true);

                    break;
                case "Result":
                    lineRenderer.gameObject.SetActive(false);
                    uiMessage.text = $"META AIVI Result {StationStageIndex.stageIndex}/{dataStages.Count - 1}";
                    inferenceUI.SetActive(false);
                    if (StationStageIndex.metaInferenceRule)
                    {
                        backgroundTopText.text = "OK";
                        backgroundTopResult.color = new Color((float)76 / 255, (float)175 / 255, (float)80 / 255);
                        if (StationStageIndex.stageIndex >= dataStages.Count - 1)
                        {
                            StationStageIndex.FinalUI = true;
                        }
                    }
                    else
                    {
                        if (StationStageIndex.stageIndex * 2 - 1 == ARCameraScript.Instance.inferenceClass &&
                            StationStageIndex.stageIndex >= dataStages.Count - 1)
                        {
                            backgroundTopText.text = "OK";
                            backgroundTopResult.color = new Color((float)76 / 255, (float)175 / 255, (float)80 / 255);
                            StationStageIndex.FinalUI = true;
                        }
                        else
                        {
                            backgroundTopText.text = "NG";
                            backgroundTopResult.color = new Color((float)244 / 255, (float)67 / 255, (float)54 / 255);
                        }
                    }

                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            uiMessage.text = ex.Message;
        }
    }

    private void DisableMainUI()
    {
        checkList.gameObject.SetActive(false);
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
                uiMessage.text = $"{MetaService.qrMetaData[2]} \n States Overview ";
                //objects = GameObject.FindGameObjectsWithTag("Checkmark");
                //foreach (GameObject obj in objects)
                //{
                //    obj.gameObject.SetActive(false);
                //}
            }
        }
    }
}