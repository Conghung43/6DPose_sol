//using System.Threading.Tasks;
//using Unity.Barracuda;

//public class NNetwork
//{
//    protected Model model;                 // Runtime model wrapper (binary)
//    protected IWorker modelWorker;       // Barracuda worker for inference
//    //private String[] outputNames;

//    public NNetwork(NNModel modelSource)
//    {
//        //this.outputNames = outputNames;

//        model = ModelLoader.Load(modelSource);
//        ModelBuilder builder = new ModelBuilder(model);
//        modelWorker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, builder.model);
//    }

//    public async Task ForwardAsyncAlt(Tensor inputs)
//    {
//        modelWorker.Execute(inputs);
//        await Task.Yield();

//        ARCameraScript.outputTensor = modelWorker.PeekOutput() ;

//        ARCameraScript.inferenceResponseFlag = true;

//    }

//    public virtual void Dispose()
//    {
//        modelWorker?.Dispose();
//    }
//}
