using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenShotReport : MonoBehaviour
{
    public Button captureButton;
    public GameObject capturePage;
    public GameObject skipBtn;
    public GameObject nextBtn;
    public RawImage captureImage;
    public Image resultImage;
    public TMPro.TextMeshProUGUI resultText;

    // public Button redoButton;
    // Start is called before the first frame update
    void Start(){
        captureButton.onClick.AddListener(RaiseButtonClick);
    }

    private void RaiseButtonClick()
    {
        Texture2D originalTexture = VisionOSCameraManager.Instance.GetMainCameraTexture2D();
        Texture2D copiedTexture = new Texture2D(
            originalTexture.width,
            originalTexture.height,
            originalTexture.format,
            false
        );
        copiedTexture.SetPixels(originalTexture.GetPixels());
        copiedTexture.Apply();
        captureImage.texture = copiedTexture;

        capturePage.SetActive(true);
        skipBtn.SetActive(!StationStageIndex.metaInferenceRule);
        nextBtn.SetActive(StationStageIndex.metaInferenceRule);
        if (StationStageIndex.metaInferenceRule)
        {
            resultText.text = "OK";
            resultImage.color = new Color((float)24 / 255, (float)175 / 255, (float)121 / 255);
        }
        else
        {
            resultText.text = "NG";
            resultImage.color = new Color((float)255 / 255, (float)89 / 255, (float)89 / 255);
        }
    }
}
