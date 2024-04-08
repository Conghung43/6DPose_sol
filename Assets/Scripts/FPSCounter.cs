using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    private float deltaTime = 0.0f;
    private GUIStyle style = new GUIStyle();

    // Customize the display position and style if needed.
    private Rect fpsRect;

    private void Start()
    {
        // Define the display position and style.
        fpsRect = new Rect(10, 10, 100, 20);
        style.fontSize = 20;
        style.normal.textColor = Color.white;
    }

    private void Update()
    {
        // Calculate delta time and update FPS.
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    private void OnGUI()
    {
        // Calculate and display FPS.
        float fps = 1.0f / deltaTime;
        string text = string.Format("FPS: {0:0.}", fps);
        GUI.Label(fpsRect, text, style);
    }
}
