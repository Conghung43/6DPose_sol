using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class GameObjectController : MonoBehaviour
{
    public GameObject barcode;
    public ARCameraScript arCameraScript;
    public GameObject ImageTarget;

    public List<GameObject> objectList;
    public Line detectionLine;
    public GameObject _sphereDetection;

    void Start()
    {
        StationStageIndex.OnFunctionIndexChange += OnGameObjectControllerFunctionChangeHandler;
    }
    void OnDisable(){
        StationStageIndex.OnFunctionIndexChange -= OnGameObjectControllerFunctionChangeHandler;
    }
    void OnEnable(){
        StationStageIndex.OnFunctionIndexChange += OnGameObjectControllerFunctionChangeHandler;
    }

    public void TurnOnAnimation()
    {
        objectList[StationStageIndex.stageIndex - 1].SetActive(true);
        if (StationStageIndex.stageIndex == 0)// for the case click next from 4 to 1 
        {
            objectList[3].SetActive(false);
        }
        else if (StationStageIndex.stageIndex < 4)// for the case click next from 4 to 1
        {
            objectList[StationStageIndex.stageIndex].SetActive(false);
        }




    }

    private void TurnOffAnimation()
    {
        objectList[StationStageIndex.stageIndex - 1].SetActive(false);
    }

    private void OnGameObjectControllerFunctionChangeHandler(string functionName){
        if (StationStageIndex.metaTimeCount != null){
            StationStageIndex.metaTimeCount.Stop();
            //Add to total time
            StationStageIndex.metaTotalMinute += StationStageIndex.metaTimeCount.Elapsed.Minutes;
            StationStageIndex.metaTotalSecond += StationStageIndex.metaTimeCount.Elapsed.Seconds;
            StationStageIndex.metaTempMinute = StationStageIndex.metaTimeCount.Elapsed.Minutes;
            StationStageIndex.metaTempSecond = StationStageIndex.metaTimeCount.Elapsed.Seconds;
            StationStageIndex.metaTimeCount = null;
            if (StationStageIndex.metaTotalSecond > 60){
                StationStageIndex.metaTotalSecond -= 60;
                StationStageIndex.metaTotalMinute += 1;
            }
        }
        switch (functionName){
            case "Home":// 2 button: Main demo or show 3d model. future approach: using vuforia area target in background
                barcode.SetActive(false);
                //ImageTarget.SetActive(false);
                break;
            case "ScanBarcode":// Show square bounding box
                StationStageIndex.barcodeFiixOn = false;
                StationStageIndex.barcodeMetaOn = false;
                StationStageIndex.stageIndex = 0;
                barcode.SetActive(true);
                break;
            case "VuforiaTarget":// Image target: all 3D model show up
                barcode.SetActive(false);
                StationStageIndex.stageIndex = 0;
                break;
            case "Sample":// show single 3D model
                if (StationStageIndex.stageIndex == 4)
                {
                    ImageTarget.SetActive(true);
                }
                else
                {
                    ImageTarget.SetActive(false);
                    StationStageIndex.imageTargetFound = false;
                }
                TurnOnAnimation();
                detectionLine.SetStart(_sphereDetection.transform);
                break;
            case "Detect":
                StationStageIndex.metaInferenceRule = false;
                ARCameraScript.inferenceResponseFlag = true;
                StationStageIndex.FinalUI = false;
                StationStageIndex.metaTimeCount = new Stopwatch();
                StationStageIndex.metaTimeCount.Start();
                TurnOffAnimation();
                //detectionLine.SetHideLine();
                break;
            case "Result":
                break;
            default:
                break;
        }
    }
}
