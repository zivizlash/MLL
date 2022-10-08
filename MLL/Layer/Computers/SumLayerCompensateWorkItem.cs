namespace MLL.Layer.Computers;

public class SumLayerCompensateWorkItem : IHasExecuteDelegate
{
    public LayerWeightsData Layer;
    public float[] Input;
    public float LearningRate;
    public float[] Errors;
    public float[] Outputs;
    public int ProcessingCount;
    public int Index;
    public CountdownEvent Countdown;

    public WaitCallback ExecuteDelegate { get; }

    public SumLayerCompensateWorkItem()
    {
        ExecuteDelegate = Execute;
    }

    public void Execute(object? _)
    {
        var (start, end) = ThreadTools.Loop(ProcessingCount, Index);

        var neurons = Layer.Neurons;

        for (int ni = start; ni < end; ni++)
        {
            var weights = neurons[ni];
            var generalError = GetGeneralError(LearningRate, Errors[ni]);

            for (int wi = 0; wi < weights.Length; wi++)
            {
                weights[wi] += generalError * Input[wi];
            }
        }

        Countdown.Signal();
    }

    private static float GetGeneralError(float learningRate, float error)
    {
        return learningRate * error;
    }
}
