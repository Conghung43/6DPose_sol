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

        Rect objectRect = arCameraScript.GetObjectBBox();
        if (_handImage.gameObject.activeInHierarchy && objectRect.Contains(_handImage.anchoredPosition))
        {
            Debug.Log("bbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
            _time -= Time.deltaTime;
            if (_time <= 0)
            {
                Debug.Log("aaaaaaaaaaaaaaaaaaaaaaaaa");
                _isChecked = true;
                _time = 0.5f;
                _detecionLine.SetHideLine();
                _nextStep.RaiseButtonClick();
            }
        }
    }

    
}
