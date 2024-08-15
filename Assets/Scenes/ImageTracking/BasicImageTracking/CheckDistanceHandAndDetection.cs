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
    private bool _isChecked;
    private float _time = 0.5f;

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
        if (_isChecked)
        {
            return;
        }

        
        if (StationStageIndex.FunctionIndex=="Sample"&&_handImage.gameObject.activeInHierarchy && arCameraScript._dectionRect.Contains(_handImage.anchoredPosition))
        {
            _time -= Time.deltaTime;
            if (_time <= 0)
            {
                _isChecked = true;
                _time = 0.5f;
                _detecionLine.SetStartAndHideLine(_paimSphere,5f);
                _detectionSphere.gameObject.SetActive(false);
                _nextStep.RaiseButtonClick();
            }
        }
    }

    
}
