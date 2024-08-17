using System;
using System.Collections.Generic;
using Mediapipe;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Rect = UnityEngine.Rect;

public class PlaceImage : MonoBehaviour
{
    public RectTransform canvasRectTransform;
    public RectTransform imageRectTransform;
    public RectTransform _rawImage;
    [SerializeField] private GameObject _sphere;
    public Camera camera;
    private Canvas canvas;
    [FormerlySerializedAs("_image")] [SerializeField] private RectTransform _Bboximage;
    public static Rect Handbbox;

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

        float pixelX = (1-x) * canvasRectTransform.rect.width;
        float pixelY = (1-y) * canvasRectTransform.rect.height;

        imageRectTransform.anchoredPosition = new Vector2(pixelX, pixelY);
        _sphere.transform.position = camera.ScreenToWorldPoint(new Vector3(pixelX, pixelY,0.5f));
    }

    public void Off()
    {
        imageRectTransform.gameObject.SetActive(false);
        _sphere.SetActive(false);
        _Bboximage.gameObject.SetActive(false);
    }

    public void DrawBBox(LocationData.Types.RelativeBoundingBox locationDataRelativeBoundingBox)
    {
        _Bboximage.gameObject.SetActive(true);
        var x = (1 - locationDataRelativeBoundingBox.Xmin)* canvasRectTransform.rect.width;
        var y = (1 - locationDataRelativeBoundingBox.Ymin)* canvasRectTransform.rect.height;
        var w = locationDataRelativeBoundingBox.Width * canvasRectTransform.rect.width;
        var h = locationDataRelativeBoundingBox.Height * canvasRectTransform.rect.height;
        _Bboximage.anchoredPosition = new Vector2(x-w ,
            y-h);
        _Bboximage.sizeDelta = new Vector2(w, h);
        Handbbox = new Rect(x - w, y - h, w, h);

    }
    public static Rect GetBoundingBox(List<Vector2> points)
    {
        if (points == null || points.Count == 0)
        {
            return new Rect();
        }

        float minX = points[0].x;
        float minY = points[0].y;
        float maxX = points[0].x;
        float maxY = points[0].y;

        // Iterate through the points to find the min and max bounds
        foreach (var point in points)
        {
            if (point.x < minX)
                minX = point.x;
            if (point.y < minY)
                minY = point.y;
            if (point.x > maxX)
                maxX = point.x;
            if (point.y > maxY)
                maxY = point.y;
        }

        // Create and return the bounding box Rect
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }
}