using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.UI.GridLayoutGroup;

public class ModelController : MonoBehaviour
{
    //private DefaultObserverEventHandler observer;
    private MeshRenderer meshRenderer;
    private List<Datastage> dataStages;
    private GameObject ImageTarget;
    void Start()
    {
        // Subscribe to events
        EventManager.OnStageChange += OnStageChangeHandler;
        StationStageIndex.OnFunctionIndexChange += OnFunctionIndexChange;
        EventManager.OnCheckpointUpdateEvent += OnUpdatePosition;
        // Get MeshRenderer component
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        ImageTarget = GameObject.Find("ImageTargetSphere");
    }

    // Event handler for the OnStageChange event
    private void OnStageChangeHandler(object sender, EventManager.OnStageIndexEventArgs e)
    {
        //Debug.Log("Stage Name = " + e.stageName);

        // Check if the stage name matches or is contained in the game object's name
        if (gameObject.name == e.stageName || gameObject.name.Contains(e.stageName))
        {
            //Debug.Log("OnPlayAnimation");
            gameObject.SetActive(true);

            // Store the position and bounds size of the game object
            if (StationStageIndex.imageTargetFound && StationStageIndex.stageIndex == 4)
            {
                StationStageIndex.stagePosition = ImageTarget.transform.position;
            }
            else
            {
                StationStageIndex.stagePosition = gameObject.transform.position;
            }
            StationStageIndex.stageMeshRenderBoundSize = meshRenderer.bounds.size;
        }
        else
        {
            // Hide the game object if the stage name doesn't match
            gameObject.SetActive(false);
            return;
        }
    }

    // Event handler for the OnFunctionIndexChange event
    private void OnFunctionIndexChange(string functionIndex)
    {
        //Debug.Log(functionIndex);

        // Check if functionIndex is "VuforiaTarget" to activate the game object
        if (functionIndex == "VuforiaTarget")
        {
            gameObject.SetActive(true);
        }

        // Activate the game object if ImageTargetFound is true and its name matches
        if (StationStageIndex.ModelTargetFound && gameObject.name == StationStageIndex.stageName)
        {
            //Debug.Log("OnFunctionIndexChange");
            gameObject.SetActive(true);

            if (StationStageIndex.imageTargetFound && StationStageIndex.stageIndex == 4)
            {
                StationStageIndex.stagePosition = ImageTarget.transform.position;
            }
            else
            {
                StationStageIndex.stagePosition = gameObject.transform.position;
            }
        }
    }

    private void OnUpdatePosition(object sender, EventManager.OnCheckpointUpdateEventArgs e)
    {

        // Activate the game object if ImageTargetFound is true and its name matches
        if (StationStageIndex.ModelTargetFound && gameObject.name == StationStageIndex.stageName)
        {
            if (StationStageIndex.imageTargetFound && StationStageIndex.stageIndex == 4)
            {
                StationStageIndex.stagePosition = ImageTarget.transform.position;
            }
            else
            {
                StationStageIndex.stagePosition = gameObject.transform.position;
            }
        }

        // Store the game object's position and scale in corners array
        Vector3[] corners = new Vector3[2];
        corners[0] = gameObject.transform.position;
        if (StationStageIndex.imageTargetFound && StationStageIndex.stageIndex == 4)
        {
            corners[0] = ImageTarget.transform.position;
        }
        corners[1] = gameObject.transform.localScale;
        // Check if dataStages is null and initialize it
        if (dataStages == null)
        {
            dataStages = ConfigRead.configData.DataStation[StationStageIndex.stationIndex].Datastage;
        }

        // Update or add the game object's corners based on its stage name
        foreach (Datastage dataStage in dataStages)
        {
            if (dataStage.StageName == gameObject.name)
            {
                if (StationStageIndex.gameObjectPoints.ContainsKey($"{dataStage.Agrs.Order}"))
                {
                    // Key exists, update the value
                    StationStageIndex.gameObjectPoints[$"{dataStage.Agrs.Order}"] = corners;
                    break;
                }
                else
                {
                    // Key does not exist, add a new key-value pair
                    StationStageIndex.gameObjectPoints.Add($"{dataStage.Agrs.Order}", corners);
                }
                break;
            }
        }
    }
}
