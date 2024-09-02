using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckDistanceHandAndDetection : MonoBehaviour
{
    [SerializeField] private RectTransform _handImage;
    [SerializeField] private ARCameraScript arCameraScript;
    [SerializeField] private NextStep _nextStep;
    [SerializeField] private Line _detecionLine;
    [SerializeField] private Transform _paimSphere;
    [SerializeField] private GameObject _detectionSphere;
    [SerializeField] private RectTransform _detectionImage;
    private bool _isChecked;
    private float _time = 0.5f;
    int stateNumber = 0;

    public void Init()
    {
        _isChecked = false;
    }
    private void Start()
    {
        Init();
    }

    private void Update()
    {
        if (_isChecked || StationStageIndex.FunctionIndex!="Sample")
        {
            return;
        }

        
        if (_detectionImage.gameObject.activeInHierarchy&&_handImage.gameObject.activeInHierarchy
            && PlaceImage.Handbbox.Contains(_detectionImage.anchoredPosition))
        {
            _time -= Time.deltaTime;
            if (_time <= 0 && stateNumber != StationStageIndex.stageIndex)
            {
                stateNumber = StationStageIndex.stageIndex;
                _isChecked = true;
                _time = 0.5f;
                _detecionLine.SetStartAndHideLine(_paimSphere,5f);
                _detectionSphere.gameObject.SetActive(false);
                _detectionImage.gameObject.SetActive(false);
                _nextStep.RaiseButtonClick();
            }
        }
    }

    
}
