using System.Collections;
//using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
//using System.Diagnostics;
using System;
using System.IO;

public class EdgeInferenceBarracuda : MonoBehaviour
{
    private bool isRunningInference = false;
    protected Model model;                 // Runtime model wrapper (binary)
    protected IWorker modelWorker;       // Barracuda worker for inference
    public NNModel modelAsset;
    [SerializeField] private TMPro.TextMeshProUGUI logInfo;
    public static int stepsPerFrame = 30;
    //private Stopwatch inferenceWatch = new Stopwatch();
    //int counttime = 0;
    public static long elMs;
    ModelBuilder builder;
    public void Start()
    {
        //this.outputNames = outputNames;

        model = ModelLoader.Load(modelAsset);
        builder = new ModelBuilder(model);
        modelWorker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, builder.model);
        EventManager.OnCheckpointUpdateEvent += StartContinuousInference;
    }

    public static float speedRealCameraAR()
    {
        Vector3 velocity = Camera.main.velocity;
        return velocity.magnitude;
    }

    public static bool isSpeedRealCameraARFast(float allowableValue)
    {
        return speedRealCameraAR() > allowableValue;
    }

    IEnumerator ImageRecognitionCoroutine()
    {
        isRunningInference = true;

        //var input = new Tensor(1, 224, 224, 3, ARCameraScript.ImageFloatValues);

        //counttime += 1;

        //if (counttime % 30 == 0)
        //{
        //    stepsPerFrame += 1;
        //}

        var input = new Tensor(ARCameraScript.resizeTextureOnnx, 3);

        //ARCameraScript.ImageFloatValues = null;

        var enumerator = modelWorker.StartManualSchedule(input);
        int step = 0;
        //inferenceWatch.Start();

        while (enumerator.MoveNext())
        {
            //logInfo.text += step.ToString();
            if (++step % stepsPerFrame == 0) yield return null;
        }

        //inferenceWatch.Stop();
        //elMs = inferenceWatch.ElapsedMilliseconds;
        //inferenceWatch.Reset();

        var output = modelWorker.PeekOutput();
        ARCameraScript.prb = output.ToReadOnlyArray();

        DateTime now = DateTime.Now;

        // Format date and time information
        string dateTimeInfo = now.ToString("yyyy-MM-dd HH:mm:ss");

        // Create log message with date and time information
        //string logMessage = $"[{dateTimeInfo}] {message}";
        logInfo.text = dateTimeInfo + " Step per frame = " + stepsPerFrame.ToString();//string.Join(", ", ARCameraScript.prb);

        //File.WriteAllBytes("test2.png", ConvertTensorToByteArray(input));


        ARCameraScript.inferenceResponseFlag = true;
        input.Dispose();
        output.Dispose();

        isRunningInference = false;
    }

    public void StartContinuousInference(object sender, EventManager.OnCheckpointUpdateEventArgs e)
    {
        if (!isRunningInference && StationStageIndex.FunctionIndex == "Detect")
        {
            //Stopwatch stopwatch = new Stopwatch(); stopwatch.Start();
            //RunContinuousInference();
            StartCoroutine(ImageRecognitionCoroutine());
            //ImageRecognitionCoroutine();
        }
    }

    public void StopContinuousInference()
    {
        isRunningInference = false;
    }

    private void OnDestroy()
    {
        // Đảm bảo giải phóng tài nguyên của worker khi không cần thiết
        if (modelWorker != null)
        {
            modelWorker.Dispose();
        }
    }
}
