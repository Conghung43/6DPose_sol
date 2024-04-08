using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DatasetCollection : MonoBehaviour
{

    public GameObject checkListGameObject;
    private Transform cpGameObject;
    private Toggle checkMarkToggle;
    // Start is called before the first frame update
    void Start()
    {
        EventManager.OnFinishImageCropEvent += OnFinishImageCropHandler;
    }

    // Update is called once per frame
    void OnFinishImageCropHandler(object sender, EventManager.OnFinishImageCropEventArgs e)
    {
        cpGameObject = checkListGameObject.transform.Find("CP" + StationStageIndex.stageIndex.ToString());
        checkMarkToggle = cpGameObject.GetComponent<Toggle>();
        if (checkMarkToggle != null )
        {
            int classIndex;
            if (checkMarkToggle.isOn)
            {
                classIndex = StationStageIndex.stageIndex *2-1;
            }
            else
            {
                classIndex = StationStageIndex.stageIndex * 2;
            }
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            //Debug.Log(timestamp);
            NativeGallery.SaveImageToGallery(e.texture2D.EncodeToJPG(), classIndex.ToString(), timestamp + ".jpg");
        }
    }
}
