using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;


public class PointCloudTracking : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The ARCameraManager which will produce frame events.")]
    ARCameraManager m_CameraManager;

    /// <summary>
    /// GetKey or set the <c>ARCameraManager</c>.
    /// </summary>
    public ARCameraManager cameraManager
    {
        get => m_CameraManager;
        set => m_CameraManager = value;
    }

    [SerializeField]
    RawImage m_RawCameraImage;

    /// <summary>
    /// The UI RawImage used to display the image on screen.
    /// </summary>
    public RawImage rawCameraImage
    {
        get => m_RawCameraImage;
        set => m_RawCameraImage = value;
    }

    [SerializeField]
    [Tooltip("The AROcclusionManager which will produce human depth and stencil textures.")]
    //public AROcclusionManager m_OcclusionManager;

    public AROcclusionManager occlusionManager;


    [SerializeField]
    RawImage m_RawEnvironmentDepthImage;

    /// <summary>
    /// The UI RawImage used to display the image on screen.
    /// </summary>
    public RawImage rawEnvironmentDepthImage
    {
        get => m_RawEnvironmentDepthImage;
        set => m_RawEnvironmentDepthImage = value;
    }

    [SerializeField]
    RawImage m_RawEnvironmentDepthConfidenceImage;

    /// <summary>
    /// The UI RawImage used to display the image on screen.
    /// </summary>
    public RawImage rawEnvironmentDepthConfidenceImage
    {
        get => m_RawEnvironmentDepthConfidenceImage;
        set => m_RawEnvironmentDepthConfidenceImage = value;
    }

    public static float depth;
    public static bool uploadDepthImage;


    XRCpuImage.Transformation m_Transformation = XRCpuImage.Transformation.MirrorY;

    /// <summary>
    /// Cycles the image transformation to the next case.
    /// </summary>
    public void CycleTransformation()
    {
        m_Transformation = m_Transformation switch
        {
            XRCpuImage.Transformation.None => XRCpuImage.Transformation.MirrorX,
            XRCpuImage.Transformation.MirrorX => XRCpuImage.Transformation.MirrorY,
            XRCpuImage.Transformation.MirrorY => XRCpuImage.Transformation.MirrorX | XRCpuImage.Transformation.MirrorY,
            _ => XRCpuImage.Transformation.None
        };
    }


    void OnEnable()
    {
        if (m_CameraManager != null)
        {
            m_CameraManager.frameReceived += OnCameraFrameReceived;
        }
        //CycleTransformation();
        rawEnvironmentDepthImage.rectTransform.sizeDelta = new Vector2(Screen.width / 5, Screen.height / 5);
    }

    void OnDisable()
    {
        if (m_CameraManager != null)
        {
            m_CameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }
    unsafe void UpdateCameraImage()
    {
        // Attempt to get the latest M_Camera image. If this method succeeds,
        // it acquires a native resource that must be disposed (see below).
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            return;
        }

        // Display some information about the M_Camera image
        //m_ImageInfo.text = string.Format(
        //    "Image info:\n\twidth: {0}\n\theight: {1}\n\tplaneCount: {2}\n\ttimestamp: {3}\n\tformat: {4}",
        //    image.width, image.height, image.planeCount, image.timestamp, image.format);

        // Once we have a valid XRCpuImage, we can access the individual image "planes"
        // (the separate channels in the image). XRCpuImage.GetPlane provides
        // low-overhead access to this data. This could then be passed to a
        // computer vision algorithm. Here, we will convert the M_Camera image
        // to an RGBA texture and draw it on the screen.

        // Choose an RGBA format.
        // See XRCpuImage.FormatSupported for a complete caps of supported formats.
        var format = TextureFormat.RGBA32;

        if (m_CameraTexture == null || m_CameraTexture.width != image.width || m_CameraTexture.height != image.height)
        {
            m_CameraTexture = new Texture2D(image.width, image.height, format, false);
        }

        // Convert the image to format, flipping the image across the Y axis.
        // We can also get a sub rectangle, but we'll get the full image here.
        var conversionParams = new XRCpuImage.ConversionParams(image, format, m_Transformation);

        // Texture2D allows us write directly to the raw texture data
        // This allows us to do the conversion in-place without making any copies.
        var rawTextureData = m_CameraTexture.GetRawTextureData<byte>();
        try
        {
            image.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
        }
        finally
        {
            // We must dispose of the XRCpuImage after we're finished
            // with it to avoid leaking native resources.
            image.Dispose();
        }

        // Apply the updated texture data to our texture
        m_CameraTexture.Apply();

        // Set the RawImage's texture so we can visualize it.
        m_RawCameraImage.texture = m_CameraTexture;
    }
    void UpdateEnvironmentDepthImage()
    {
        if (m_RawEnvironmentDepthImage == null)
            return;

        // Attempt to get the latest environment depth image. If this method succeeds,
        // it acquires a native resource that must be disposed (see below).
        if (occlusionManager && occlusionManager.TryAcquireEnvironmentDepthCpuImage(out var image))
        {
            using (image)
            {
                UpdateRawImage(m_RawEnvironmentDepthImage, image, m_Transformation);
            }
        }
        else
        {
            m_RawEnvironmentDepthImage.enabled = false;
        }
    }

    void UpdateEnvironmentDepthConfidenceImage()
    {
        if (m_RawEnvironmentDepthConfidenceImage == null)
            return;

        // Attempt to get the latest environment depth image. If this method succeeds,
        // it acquires a native resource that must be disposed (see below).
        if (occlusionManager && occlusionManager.TryAcquireEnvironmentDepthConfidenceCpuImage(out var image))
        {
            using (image)
            {
                UpdateRawImage(m_RawEnvironmentDepthConfidenceImage, image, m_Transformation);
            }
        }
        else
        {
            m_RawEnvironmentDepthConfidenceImage.enabled = false;
        }
    }
    public static Texture2D texture;
    void UpdateRawImage(RawImage rawImage, XRCpuImage cpuImage, XRCpuImage.Transformation transformation)
    {
        // GetKey the texture associated with the UI.RawImage that we wish to display on screen.

        if (texture == null)
            texture = new Texture2D(2, 2);
        texture = rawImage.texture as Texture2D;

        // If the texture hasn't yet been created, or if its dimensions have changed, (re)create the texture.
        // Note: Although texture dimensions do not normally change frame-to-frame, they can change in response to
        //    a change in the M_Camera resolution (for M_Camera caps) or changes to the quality of the human depth
        //    and human stencil buffers.
        if (texture == null || texture.width != cpuImage.width || texture.height != cpuImage.height)
        {
            texture = new Texture2D(cpuImage.width, cpuImage.height, cpuImage.format.AsTextureFormat(), false);
            rawImage.texture = texture;
        }

        // For display, we need to mirror about the vertical access.
        var conversionParams = new XRCpuImage.ConversionParams(cpuImage, cpuImage.format.AsTextureFormat(), transformation);

        // GetKey the Texture2D's underlying pixel buffer.
        var rawTextureData = texture.GetRawTextureData<byte>();

        // Make sure the destination buffer is large enough to hold the converted data (they should be the same size)
        Debug.Assert(rawTextureData.Length == cpuImage.GetConvertedDataSize(conversionParams.outputDimensions, conversionParams.outputFormat),
            "The Texture2D is not the same size as the converted data.");

        // Perform the conversion.
        cpuImage.Convert(conversionParams, rawTextureData);

        // "Apply" the new pixel data to the Texture2D.
        texture.Apply();

        //float depth = GetDepthValue(texture);
        //logMessage.text = depth.ToString();
        if (m_RawEnvironmentDepthImage == rawImage)
        {
            try
            {
                depth = ReadDepthValue(texture, (int)texture.width / 2, (int)texture.height / 2);

            }
            catch
            {
                //logMessage.text += ex.Message;
            }
        }
        // Make sure it's enabled.
        rawImage.enabled = true;
    }

    int DepthWidth;
    int DepthHeight;

    private float ReadDepthValue(Texture2D depthTexture, int x, int y)
    {
        // Convert the pixel coordinates to the corresponding UV coordinates
        Vector2 uv = new Vector2(x / (float)depthTexture.width, y / (float)depthTexture.height);

        // Read the depth value at the UV coordinates from the depth texture
        float depthValue = depthTexture.GetPixelBilinear(uv.x, uv.y).r;

        return depthValue;
    }

    // Obtain the depth value in meters at a normalized screen point.
    public float GetDepthFromUV(Vector2 uv, short[] depthArray)
    {
        int depthX = (int)(uv.x * (DepthWidth - 1));
        int depthY = (int)(uv.y * (DepthHeight - 1));

        return GetDepthFromXY(depthX, depthY, depthArray);
    }

    // Obtain the depth value in meters at the specified x, y location.
    public float GetDepthFromXY(int x, int y, short[] depthArray)
    {
        var depthIndex = (y * DepthWidth) + x;
        var depthInShort = depthArray[depthIndex];
        //var depthInMeters = depthInShort * MillimeterToMeter;
        return depthInShort;
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (uploadDepthImage)
        {
            uploadDepthImage = false;
            UpdateEnvironmentDepthImage();
        }
    }

    Texture2D m_CameraTexture;
}
