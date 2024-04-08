using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using System.Diagnostics;

public class EdgeInferenceBarracuda : MonoBehaviour
{
    private bool isRunningInference = false;
    protected Model model;                 // Runtime model wrapper (binary)
    protected IWorker modelWorker;       // Barracuda worker for inference
    public NNModel modelAsset;
    [SerializeField] private TMPro.TextMeshProUGUI logInfo;
    public static int stepsPerFrame = 30;
    private Stopwatch inferenceWatch = new Stopwatch();
    int counttime = 10;
    public static long elMs;
    public void Start()
    {
        //this.outputNames = outputNames;

        model = ModelLoader.Load(modelAsset);
        ModelBuilder builder = new ModelBuilder(model);
        modelWorker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, builder.model);
        EventManager.OnCheckpointUpdateEvent += StartContinuousInference;
    }

    private void RunContinuousInference()
    {
        isRunningInference = true;
        
        if (ARCameraScript.ImageFloatValues != null)
        {
            Tensor inputTensor = new Tensor(1, 224, 224, 3, ARCameraScript.ImageFloatValues);

            var output = modelWorker.Execute(inputTensor).PeekOutput();
            ARCameraScript.prb = output.ToReadOnlyArray();

            ARCameraScript.inferenceResponseFlag = true;

            //yield return new WaitForSeconds(0.001f); 
            inputTensor.Dispose();
        }
        //yield return null;
        isRunningInference = false;
    }

    IEnumerator ImageRecognitionCoroutine()
    {
        isRunningInference = true;
        if (ARCameraScript.ImageFloatValues != null)
        {
            var input = new Tensor(1, 224, 224, 3, ARCameraScript.ImageFloatValues);
            var enumerator = modelWorker.StartManualSchedule(input);
            int step = 0;
            inferenceWatch.Start();

            while (enumerator.MoveNext())
            {
                if (++step % stepsPerFrame == 0) yield return null;
            }

            inferenceWatch.Stop();
            elMs = inferenceWatch.ElapsedMilliseconds;
            inferenceWatch.Reset();

            var output = modelWorker.PeekOutput();
            ARCameraScript.prb = output.ToReadOnlyArray();

            ARCameraScript.inferenceResponseFlag = true;
            input.Dispose();

            //logInfo.text = stepsPerFrame.ToString();
        }
        isRunningInference = false;
    }

    public void StartContinuousInference(object sender, EventManager.OnCheckpointUpdateEventArgs e)
    {
        if (!isRunningInference && StationStageIndex.FunctionIndex == "Detect")
        {
            //Stopwatch stopwatch = new Stopwatch(); stopwatch.Start();
            //RunContinuousInference();
            StartCoroutine(ImageRecognitionCoroutine());
            //stopwatch.Stop(); long elMs = stopwatch.ElapsedMilliseconds; stopwatch.Reset(); stopwatch.Start();
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
