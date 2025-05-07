using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class LoadStreamingAssetImage : MonoBehaviour
{
    public RawImage rawImage;
    public string imageName = "queryImage.jpg";

    void Start()
    {
        StartCoroutine(LoadImage());
    }

    IEnumerator LoadImage()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, imageName);
        
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(path);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Lỗi tải ảnh: " + request.error);
        }
        else
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            rawImage.texture = texture;
            rawImage.SetNativeSize();
        }
    }
}