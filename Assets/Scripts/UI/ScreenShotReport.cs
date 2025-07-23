using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenShotReport : MonoBehaviour
{
    public Button captureButton;
    public ARCameraScript arCameraScript;
    // public Button redoButton;
    // Start is called before the first frame update
    void OnDisable(){
        captureButton.onClick.RemoveListener(RaiseButtonClick);
    }
    void OnEnable(){
        captureButton.onClick.AddListener(RaiseButtonClick);
    }
    private void RaiseButtonClick()
    {
        arCameraScript.TakeScreenshot();
        //Raise Result page event
        StationStageIndex.FunctionIndex = "Result";
    }

}
