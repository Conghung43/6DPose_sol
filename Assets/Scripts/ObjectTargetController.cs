using System;
using UnityEngine;
//using Vuforia;

public class ObjectTargetController : MonoBehaviour
{
    //public DefaultObserverEventHandler observer;
    //private ObserverBehaviour mObserverBehaviour;
    //[SerializeField] private TMPro.TextMeshProUGUI logInfo;
    //void OnDisable(){
    //    observer.OnTargetFound.RemoveListener(OnTargetFound);
    //    observer.OnTargetLost.RemoveListener(OnTbjectargetLost);
    //}
    void OnEnable(){
        //mObserverBehaviour = GetComponent<ObserverBehaviour>();
        //observer = FindObjectOfType<DefaultObserverEventHandler>();
        //targetBehaviour = FindObjectOfType<ModelTargetBehaviour>();
        //observer.OnTargetFound.AddListener(OnTargetFound);
        //observer.OnTargetLost.AddListener(OnTargetLost);
        //mObserverBehaviour.OnTargetStatusChanged += OnObserverStatusChanged;
    }

    private void OnTargetFound()
    {
        if (gameObject.name.Contains("Model"))
        {
            StationStageIndex.ModelTargetFound = true;
        }
        else
        {
            StationStageIndex.imageTargetFound = true;
        }
        //observer.enabled = false;
    }
    private void OnTargetLost()
    {
        if (gameObject.name.Contains("Model"))
        {
            StationStageIndex.ModelTargetFound = false;
        }
        else
        {
            StationStageIndex.imageTargetFound = false;
        }
    }
}
