using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

//Interpolation between points with a Catmull-Rom spline
public class CatmullRomSpline : MonoBehaviour
{
	//Has to be at least 4 points
	private Vector3[] controlPointsList = TargetHitDetector.lineBasedPoints;
	//Are we making a line or a loop?
	public bool isLooping = true;

    public LineRenderer lineRenderer;
	int lineIndex = 0;
    float resolution = 0.05f;
    private Vector3 prevPosition;
    //public Camera arCamera;
    public float cameraToTargetDistance = 0;
    int currentTimeInSeconds = -1;
    //Display without having to press play
    //void OnDrawGizmos()
    //{
    //       //if (!isLooping) return;
    //	Gizmos.color = Color.white;

    //	//Draw the Catmull-Rom spline between the points
    //	for (int i = 0; i < controlPointsList.Length; i++)
    //	{
    //		//Cant draw between the endpoints
    //		//Neither do we need to draw from the second to the last endpoint
    //		//...if we are not making a looping line
    //		if ((i == 0 || i == controlPointsList.Length - 2 || i == controlPointsList.Length - 1) && !isLooping)
    //		{
    //			continue;
    //		}

    //		DisplayCatmullRomSpline(i);
    //	}
    //}
    private void OnEnable()
    {
        // Initialize the LineRenderer component
        //lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 60;
        lineRenderer.startWidth = 0.003f;
        lineRenderer.endWidth = 0.001f;
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = new Color(1.0f, 0.5f, 0.0f, 1f);
    }

    void Update()
    {
        if (StationStageIndex.FunctionIndex != "Sample")
        {
            return;
        }
        float distance = Vector3.Distance(controlPointsList[0], controlPointsList[3]);
        if (distance < 0.3f)
        {
            lineRenderer.enabled = false;
            return;
        }
        else
        {
            if (distance < 0.5f)
            {
                lineRenderer.startColor = new Color(1.0f, 0.5f, 0.0f, 1f);
                lineRenderer.endColor = new Color(1.0f, 0.5f, 0.0f, 1f);
            }
            else
            {
                lineRenderer.startColor = Color.white;
                lineRenderer.endColor = new Color(1.0f, 0.5f, 0.0f, 1f);
            }
            lineRenderer.enabled = true;
        }
        //Direct line
        if (controlPointsList[1] == controlPointsList[2])
        {
            for (int i = 0; i < lineRenderer.positionCount; i++)
            {
                float t = (float)i / (lineRenderer.positionCount - 1); // Interpolation factor between 0 and 1
                Vector3 generatedPoint = Vector3.Lerp(controlPointsList[0], controlPointsList[1], t);
                lineRenderer.SetPosition(i, generatedPoint);
            }
            return;
        }

        lineIndex = 0;

        //Draw the Catmull-Rom spline between the points
        //for (int i = 0; i < TargetHitDetector.linePartLength ; i++)
        for (int i = 0; i < controlPointsList.Length -1; i++)
        {
            //Cant draw between the endpoints
            //Neither do we need to draw from the second to the last endpoint
            //...if we are not making a looping line
            if ((i == 0 || i == controlPointsList.Length - 2 || i == controlPointsList.Length - 1) && !isLooping)
            {
                continue;
            }

            DisplayCatmullRomSpline(i);
        }
		//isLooping = !isLooping;
    }

    //Display a spline between 2 points derived with the Catmull-Rom spline algorithm
    void DisplayCatmullRomSpline(int pos)
	{
		//The 4 points we need to form a spline between p1 and p2
		Vector3 p0 = controlPointsList[ClampListPos(pos - 1)];
		Vector3 p1 = controlPointsList[pos];
		Vector3 p2 = controlPointsList[ClampListPos(pos + 1)];
		Vector3 p3 = controlPointsList[ClampListPos(pos + 2)];

		//The start position of the line
		Vector3 lastPos = p1;

		//The spline's resolution
		//Make sure it's is adding up to 1, so 0.3 will give a gap, but 0.2 will work
		

		//How many times should we loop?
		int loops = Mathf.FloorToInt(1f / resolution);

		for (int i = 1; i <= loops; i++)
		{
			//Which t position are we at?
			float t = i * resolution;

			//Find the coordinate between the end points with a Catmull-Rom spline
			Vector3 newPos = GetCatmullRomPosition(t, p0, p1, p2, p3);

            // Draw game object to build line
            //Instantiate(controlPointsList[0], newPos, Quaternion.identity);
            lineRenderer.SetPosition(lineIndex, newPos);
			lineIndex++;
                //Draw this line segment
                //Gizmos.DrawLine(lastPos, newPos);

                //Save this pos so we can draw the next line segment
                lastPos = newPos;
		}
	}

	//Clamp the list positions to allow looping
	int ClampListPos(int pos)
	{
		if (pos < 0)
		{
			pos = controlPointsList.Length - 1;
		}

		if (pos > controlPointsList.Length)
		{
			pos = 1;
		}
		else if (pos > controlPointsList.Length - 1)
		{
			pos = 0;
		}

		return pos;
	}

	//Returns a position between 4 Vector3 with Catmull-Rom spline algorithm
	//http://www.iquilezles.org/www/articles/minispline/minispline.htm
	Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
	{
		//The coefficients of the cubic polynomial (except the 0.5f * which I added later for performance)
		Vector3 a = 2f * p1;
		Vector3 b = p2 - p0;
		Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
		Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

		//The cubic polynomial: a + b * t + c * t^2 + d * t^3
		Vector3 pos = 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));

		return pos;
	}
}