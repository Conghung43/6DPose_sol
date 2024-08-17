using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NextStep : MonoBehaviour
{
    public Button nextStep;
    [SerializeField] private TMPro.TextMeshProUGUI uiMessage;
    public Toggle toggleAP;
    private Coroutine _autoNext;
    public CheckDistanceHandAndDetection _CheckDistanceHandAndDetection;
    public DescriptionController _descriptionController;
    private void OnDisable()
    {
        nextStep.onClick.RemoveListener(RaiseButtonClick);
    }

    private void OnEnable()
    {
        nextStep.onClick.AddListener(RaiseButtonClick);
    }

    public void CallAutoNextAfterDelay(float delayInSeconds)
    {
        _autoNext = StartCoroutine(CallAutoNextAfterDelayCoroutine(delayInSeconds));
    }
    // Coroutine to handle the delay
    IEnumerator CallAutoNextAfterDelayCoroutine(float delayInSeconds)
    {
        // Wait for the specified amount of time
        yield return new WaitForSeconds(delayInSeconds);

        // Call the function after the delay
        if (StationStageIndex.FunctionIndex == "Detect")
        {
            RaiseButtonClick();
        }
        
    }

    // Handle button click event
    public void RaiseButtonClick()
    {
        if (_autoNext != null)
        {
            StopCoroutine(_autoNext);
        }
        switch (StationStageIndex.FunctionIndex)
        {
            case "3dModel":
                // Show single 3D model
                EventManager.OnModelChangeButtonClick?.Invoke(this, new EventManager.OnModelChangeButtonClickEventArgs
                {
                    buttonType = true
                });
                break;
            case "VuforiaTarget":
                GotoNextState();
                break;
            case "Sample":
                StationStageIndex.FunctionIndex = "Detect";
                break;
            case "Detect":
                GotoNextState();
                break;
            case "Result":
                // Save to Fiix
                // Go to next state
                GotoNextState();
                break;
            case "ScanBarcode":
                StationStageIndex.FunctionIndex = "VuforiaTarget";
                break;
            default:
                break;
        }
        _CheckDistanceHandAndDetection.Init();
        _descriptionController.UpdateDescription(StationStageIndex.FunctionIndex, StationStageIndex.stageIndex);
    }

    // Go to the next state
    private void GotoNextState()
    {
        List<Datastage> dataStages = ConfigRead.configData.DataStation[StationStageIndex.stationIndex].Datastage;
        string jump2StageName = "";

        ARCameraScript.lastInferenceClass = -1;
        StationStageIndex.stageIndex += 1;
        
        if (StationStageIndex.stageIndex > dataStages.Count - 1)
        {
            StationStageIndex.stageIndex = 0;//dataStages.Count - 1;
            StationStageIndex.FunctionIndex = "VuforiaTarget";
            return;
        }
        StationStageIndex.FunctionIndex = "Sample"; // 6D pose state or detect state always go to sample
        if (MetaService.stageData != null)
        {
            MetaService.stageData.requestResult = false;
        }

        if (!toggleAP.isOn)
        {
            MetaService.ConnectWithMetaStageID();
        }

        foreach (Datastage dataStage in dataStages)
        {
            if (dataStage.Agrs.Order == StationStageIndex.stageIndex)
            {
                jump2StageName = dataStage.StageName;
                break;
            }
        }

        if (jump2StageName == "")
        {
            return;
        }

        StationStageIndex.stageName = jump2StageName; // Duplicate code

        EventManager.OnStageChange?.Invoke(this, new EventManager.OnStageIndexEventArgs
        {
            nextButtonClick = true,
            stageName = jump2StageName
        });
    }
}
