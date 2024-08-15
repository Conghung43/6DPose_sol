using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation.Samples;

public class UIController : MonoBehaviour
{
    public Button nextButton;
    public Button backButton;
    public Button screenShotButton;
    public Button redoButton;
    public Button skipButton;
    public Button modelTargetBtn;
    public GameObject qrCodeFrame;
    public GameObject highlightChecklist;
    public Button finishButton;
    public Button retryButton;
    public GameObject BottomBackground;
    public GameObject ResultCanvas;
    public GameObject checkList;
    public GameObject ModelTarget;
    [SerializeField]  private TMPro.TextMeshProUGUI uiMessage;
    public RawImage backgroundResult;
    public Image backgroundTopResult;
    [SerializeField]  private TMPro.TextMeshProUGUI backgroundTopText;
    private GameObject[] objects;
    public GameObject title;
    public Texture2D okImage;
    public Texture2D ngImage;
    private bool isEventAdded;
    public GameObject flowInstruction;
    public LineRenderer lineRenderer;
    public Toggle toggleOnnx;
    public GameObject inputField;
    //public DefaultObserverEventHandler observer;
    //public Vuforia.ModelTargetBehaviour targetBehaviour;
    // Start is called before the first frame update
    [SerializeField] private DescriptionController _descriptionController;

    void OnDisable(){
        StationStageIndex.OnFunctionIndexChange -= OnFunctionIndexChangeActionHandler;
        StationStageIndex.OnImageTargetFoundChange -= OnImageTargetFoundActionHandler;
        StationStageIndex.OnFinalUIChange -= OnFinalUIPage;
    }

    void OnEnable(){
        StationStageIndex.OnFunctionIndexChange += OnFunctionIndexChangeActionHandler;
        StationStageIndex.OnImageTargetFoundChange += OnImageTargetFoundActionHandler;
        StationStageIndex.OnFinalUIChange += OnFinalUIPage;
    }
    private void OnFunctionIndexChangeActionHandler(string functionName)
    {
        _descriptionController.UpdateDescription(functionName,StationStageIndex.stageIndex);
        List<Datastage> dataStages = ConfigRead.configData.DataStation[StationStageIndex.stationIndex].Datastage;
        try
        {
            switch (functionName)
            {
                case "Home":// 2 button: Main demo or show 3d model. future approach: using vuforia area target in background
                    uiMessage.text = "Home";
                    qrCodeFrame.SetActive(false);
                    //model3Dbtn.gameObject.SetActive(true);
                    modelTargetBtn.gameObject.SetActive(true);
                    ModelTarget.SetActive(false);
                    DisableMainUI();
                    lineRenderer.gameObject.SetActive(false);
                    break;
                case "ScanBarcode":// Show square bounding box
                    uiMessage.text = "Scan META QR code";
                    modelTargetBtn.gameObject.SetActive(false);
                    qrCodeFrame.SetActive(true);
                    inputField.SetActive(false);
                    DisableMainUI();
                    break;
                case "VuforiaTargetDetecting":
                    uiMessage.text = $"{MetaService.qrMetaData[2]} \n Target detecting..";
                    TurnOnInferenceFlag();
                    StationStageIndex.ModelTargetFound = false;
                    ModelTarget.gameObject.SetActive(false);
                    ModelTarget.gameObject.SetActive(true);
                    //observer.enabled = true;
                    //targetBehaviour.enabled = true;
                    DisableMainUI();
                    qrCodeFrame.SetActive(false);
                    break;
                case "VuforiaTarget":// Image target: all 3D model show up
                    //observer.enabled = true;
                    //targetBehaviour.enabled = true;
                    TurnOnInferenceFlag();
                    lineRenderer.gameObject.SetActive(false);
                    flowInstruction.SetActive(true);
                    uiMessage.text = $"{MetaService.qrMetaData[2]} \n Target detected";
                    skipButton.gameObject.SetActive(false);
                    finishButton.gameObject.SetActive(false);
                    retryButton.gameObject.SetActive(false);
                    ResultCanvas.SetActive(false);
                    BottomBackground.SetActive(false);
                    if (StationStageIndex.ModelTargetFound)
                    {
                        checkList.gameObject.SetActive(true);
                        highlightChecklist.SetActive(false);
                        nextButton.gameObject.SetActive(true);
                        uiMessage.text = $"{MetaService.qrMetaData[2]} \n Checkpoint Overview";
                        objects = GameObject.FindGameObjectsWithTag("Checkmark");
                        foreach (GameObject obj in objects)
                        {
                            obj.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        nextButton.gameObject.SetActive(false);
                        backButton.gameObject.SetActive(false);
                    }
                    break;
                case "Sample":
                    flowInstruction.SetActive(false);
                    highlightChecklist.SetActive(true);
                    ResultCanvas.SetActive(false);
                    BottomBackground.SetActive(false);
                    if (StationStageIndex.stageIndex >= dataStages.Count)
                    {
                        nextButton.gameObject.SetActive(false);
                    }
                    else
                    {
                        nextButton.gameObject.SetActive(true);
                    }
                    if (StationStageIndex.stageIndex <= 0)
                    {
                        backButton.gameObject.SetActive(false);
                    }
                    else
                    {
                        backButton.gameObject.SetActive(true);
                    }
                    skipButton.gameObject.SetActive(false);
                    redoButton.gameObject.SetActive(false);
                    screenShotButton.gameObject.SetActive(false);
                    //if (StationStageIndex.stageIndex == 4)
                    //{
                    //    observer.enabled = false;
                    //    targetBehaviour.enabled = false;
                    //}
                    uiMessage.text = $"Instruction {StationStageIndex.stageIndex}/{dataStages.Count - 1}";
                    break;
                case "Detect":
                    lineRenderer.gameObject.SetActive(false);
                    uiMessage.text = $"META AIVI {StationStageIndex.stageIndex}/{dataStages.Count - 1}";
                    redoButton.gameObject.SetActive(false);
                    backButton.gameObject.SetActive(false);
                    nextButton.gameObject.SetActive(false);
                    skipButton.gameObject.SetActive(false);
                    finishButton.gameObject.SetActive(false);
                    retryButton.gameObject.SetActive(false);
                    ResultCanvas.SetActive(false);
                    BottomBackground.SetActive(false);
                    screenShotButton.gameObject.SetActive(true);
                    break;
                case "Result":
                    lineRenderer.gameObject.SetActive(false);
                    uiMessage.text = $"META AIVI Result {StationStageIndex.stageIndex}/{dataStages.Count - 1}";
                    ResultCanvas.SetActive(true);
                    screenShotButton.gameObject.SetActive(false);
                    title.SetActive(true);
                    BottomBackground.SetActive(true);
                    if (StationStageIndex.metaInferenceRule )
                    {
                        backgroundResult.texture = okImage;
                        backgroundTopText.text = "OK";
                        backgroundTopResult.color = new Color((float)76 / 255, (float)175 / 255, (float)80 / 255);
                        if (StationStageIndex.stageIndex >= dataStages.Count - 1)
                        {
                            StationStageIndex.FinalUI = true;
                        }
                        else
                        {
                            skipButton.gameObject.SetActive(false);
                            backButton.gameObject.SetActive(true);
                            nextButton.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        if (toggleOnnx.isOn && StationStageIndex.stageIndex * 2 - 1 == ARCameraScript.inferenceClass && StationStageIndex.stageIndex >= dataStages.Count - 1)
                        {
                            backgroundResult.texture = okImage;
                            backgroundTopText.text = "OK";
                            backgroundTopResult.color = new Color((float)76 / 255, (float)175 / 255, (float)80 / 255);
                            StationStageIndex.FinalUI = true;
                        }
                        else
                        {
                            backgroundResult.texture = ngImage;
                            backgroundTopText.text = "NG";
                            backgroundTopResult.color = new Color((float)244 / 255, (float)67 / 255, (float)54 / 255);
                            redoButton.gameObject.SetActive(true);
                            skipButton.gameObject.SetActive(true);
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
        backButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
        screenShotButton.gameObject.SetActive(false);
        redoButton.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(false);
        finishButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);
        BottomBackground.SetActive(false);
        ResultCanvas.SetActive(false);
    }

    public void OnFinalUIPage(bool finalUI)
    {
        if (finalUI){
            BottomBackground.SetActive(true);
            finishButton.gameObject.SetActive(true);
            retryButton.gameObject.SetActive(true);
            redoButton.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
        }
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
            nextButton.gameObject.SetActive(true);
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

