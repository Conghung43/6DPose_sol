using System;
using System.Collections;
using Mediapipe.Unity.Sample;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using UnityEngine.XR.ARFoundation.Samples;

public class CameraFeedToRenderTexture : MonoBehaviour
{
    public TrackedImageInfoManager _trackedImageInfoManager;
    public Solution _solution;
    public AppSettings _AppSettings;
    public static CameraFeedToRenderTexture instance;
    public ARCameraManager arCameraManager;
    public RenderTexture renderTexture;
    private Texture2D cameraTexture;

    private void Awake()
    {
        instance = this;
    }

    void OnEnable()
    {
        renderTexture = new RenderTexture(_AppSettings._defaultAvailableWebCamResolutions[0].width, _AppSettings._defaultAvailableWebCamResolutions[0].height, 24);
        
        StartCoroutine(SelectResolution(2f));
    }

    private IEnumerator SelectResolution(float second)
    {
        yield return new WaitForSeconds(second);
        
        if (_solution != null)
        {
            _solution.Pause();
        }
        var imageSource = ImageSourceProvider.ImageSource;
        imageSource.SelectResolution(1);
        if (_solution != null)
        {
            _solution.Play();
        }
    }

    

    public void UpdateTexture()
    {
        
        WriteTextureToRenderTexture(TrackedImageInfoManager.handTexture, renderTexture);
    }

    void WriteTextureToRenderTexture(Texture2D texture, RenderTexture renderTexture)
    {
        RenderTexture.active = renderTexture;
        Graphics.Blit(texture, renderTexture);
        RenderTexture.active = null;
    }

    void OnDestroy()
    {
        if (cameraTexture != null)
        {
            Destroy(cameraTexture);
        }
    }
}