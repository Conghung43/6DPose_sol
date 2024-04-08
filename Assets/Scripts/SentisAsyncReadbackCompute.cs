//using System.Collections;
//using System.Collections.Generic;
//using System.Diagnostics;
//using Unity.Sentis;
//using UnityEngine;

//public class SentisAsyncReadbackCompute : MonoBehaviour
//{
//    [SerializeField]
//    ModelAsset modelAsset;

//    Tensor m_Input;
//    IWorker m_Engine;
//    TensorFloat m_OutputTensor;


//    private bool isRunningInference = false;
//    [SerializeField] private TMPro.TextMeshProUGUI logInfo;
//    public static int stepsPerFrame = 30;
//    private System.Diagnostics.Stopwatch inferenceWatch = new Stopwatch();
//    int counttime = 10;
//    public static long elMs;



//    void ReadbackCallback(bool completed)
//    {
//        // The call to `MakeReadable` will no longer block with a readback as the data is already on the CPU
//        m_OutputTensor.MakeReadable();
//        // The output tensor is now in a readable state on the CPU
//        ARCameraScript.prb = m_OutputTensor.ToReadOnlyArray();

//        ARCameraScript.inferenceResponseFlag = true;
//    }

//    void OnEnable()
//    {
//        var model = ModelLoader.Load(modelAsset);
        
//        m_Engine = WorkerFactory.CreateWorker(BackendType.GPUCompute, model);

//        EventManager.OnCheckpointUpdateEvent += StartContinuousInference;




//        // Continue to run code on the main thread while waiting for the tensor readback
//    }

//    void ImageRecognitionCoroutine()
//    {
//        if (ARCameraScript.ImageFloatValues != null)
//        {
//            m_Input = new TensorFloat(new TensorShape(1, 224, 224, 3), ARCameraScript.ImageFloatValues);
//            m_Engine.Execute(m_Input);

//            // Peek the value from Sentis, without taking ownership of the tensor
//            m_OutputTensor = m_Engine.PeekOutput() as TensorFloat;
//            m_OutputTensor.AsyncReadbackRequest(ReadbackCallback);


//        }
//    }

//    public void StartContinuousInference(object sender, EventManager.OnCheckpointUpdateEventArgs e)
//    {
//        if (StationStageIndex.FunctionIndex == "Detect")
//        {
//            //Stopwatch stopwatch = new Stopwatch(); stopwatch.Start();
//            //RunContinuousInference();
//            ImageRecognitionCoroutine();
//            //stopwatch.Stop(); long elMs = stopwatch.ElapsedMilliseconds; stopwatch.Reset(); stopwatch.Start();
//        }
//    }

//    void OnDisable()
//    {
//        m_Input.Dispose();
//        m_Engine.Dispose();
//    }
//}