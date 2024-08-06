using System;
using UnityEngine;
using UnityEngine.UI;

public class PlaceImage : MonoBehaviour
{
    public RectTransform canvasRectTransform;
    public RectTransform imageRectTransform;
    public RectTransform _rawImage;
    [SerializeField] private GameObject _sphere;
    public Camera camera;
    private Canvas canvas;

    private void Start()
    {
#if UNITY_EDITOR
        _rawImage.localScale = new Vector3(-1f, 1f, 1f);
#endif
    }

    public void Draw(float x, float y)
    {
        imageRectTransform.gameObject.SetActive(true);
        _sphere.SetActive(true);
#if !UNITY_EDITOR
        float pixelX = (1-x) * canvasRectTransform.rect.width;
        float pixelY = (1-y) * canvasRectTransform.rect.height;
#else
        float pixelX = x * canvasRectTransform.rect.width;
        float pixelY = (1-y) * canvasRectTransform.rect.height;
#endif
        imageRectTransform.anchoredPosition = new Vector2(pixelX, pixelY);
        _sphere.transform.position = camera.ScreenToWorldPoint(new Vector3(pixelX, pixelY,0.5f));
    }

    public void Off()
    {
        imageRectTransform.gameObject.SetActive(false);
        _sphere.SetActive(false);
    }
}