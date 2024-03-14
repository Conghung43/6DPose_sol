using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ImageProcessing
{
    public static Texture2D CropTexture2D(Texture2D sourceTexture,Texture2D textureCrop, Rect cropRect)
    {
        textureCrop.SetPixels(sourceTexture.GetPixels((int)cropRect.x, (int)cropRect.y, (int)cropRect.width, (int)cropRect.height));
        textureCrop.Apply();
        return textureCrop;
    }

    /**
    * Transforms a point from an Xr Camera image to a screen point.
    */
    public static bool XrImagePointToScreenPoint(Vector2 xrPoint, out Vector2 screenPoint, Vector2 xrImageSize, Vector2 screenSize)
    {
        var xrToScreenRatio = screenSize.x / xrImageSize.x;
        var theoricalXrHeight = xrImageSize.y * xrToScreenRatio;

        var exceededHeight = theoricalXrHeight - screenSize.y;
        var margin = exceededHeight / 2;

        var x = xrPoint.x * xrToScreenRatio;
        var y = xrPoint.y * xrToScreenRatio - margin;

        if (x < 0 || x > screenSize.x)
        {
            screenPoint = Vector2.zero;
            return false;
        }

        screenPoint = new Vector2((int)x, (int)y);

        return true;
    }

    /**
* Transforms a point from an Xr Camera image to a screen point.
*/
    public static bool ScreenPointToXrImagePoint(Vector2 xrPoint, out Vector2 screenPoint, Vector2 xrImageSize, Vector2 screenSize)
    {
        var xrToScreenRatio =   xrImageSize.x/ screenSize.x;
        var theoricalXrHeight = screenSize.y * xrToScreenRatio;

        var exceededHeight = xrImageSize.y - theoricalXrHeight;
        var margin = exceededHeight / 2;

        var x = 0;
        var y = margin;

        if (x < 0 || x > screenSize.x)
        {
            screenPoint = Vector2.zero;
            return false;
        }

        screenPoint = new Vector2((int)x, (int)y);

        return true;
    }

}
