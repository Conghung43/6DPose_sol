using UnityEngine;
using UnityEngine.UI;

public class PlaceImage : MonoBehaviour
{
    public RectTransform canvasRectTransform;
    public RectTransform imageRectTransform;
    [SerializeField] private GameObject _sphere;
    public Camera camera;
    private Canvas canvas;
   

    public void Draw(float x, float y)
    {
        imageRectTransform.gameObject.SetActive(true);
        _sphere.SetActive(true);
        float pixelX = (1-x) * canvasRectTransform.rect.width;
        float pixelY = (y) * canvasRectTransform.rect.height;
        imageRectTransform.anchoredPosition = new Vector2(pixelX, pixelY);
        _sphere.transform.position = camera.ScreenToWorldPoint(new Vector3(pixelX, pixelY,0.5f));
    }

    public void Off()
    {
        imageRectTransform.gameObject.SetActive(false);
        _sphere.SetActive(false);
    }
}