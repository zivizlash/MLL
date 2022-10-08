using MLL.Tools;

namespace MLL.Layer.Computers;

public class SumLayerPredictWorkItem : IHasExecuteDelegate
{
    public LayerWeightsData Layer;
    public float[] Input;
    public float[] Results;
    public int ProcessingCount;
    public int Index;
    public CountdownEvent Countdown;

    public WaitCallback ExecuteDelegate { get; }

    public SumLayerPredictWorkItem()
    {
        ExecuteDelegate = Execute;
    }

    public void Execute(object? _)
    {
        var neurons = Layer.Neurons;

        var (start, end) = ThreadTools.Loop(ProcessingCount, Index);

        for (int ni = start; ni < end; ni++)
        {
            var sum = VectorCalculator.CalculateMultiplySum(neurons[ni], Input);
            Results[ni] = sum;
        }

        Countdown.Signal();
    }
}
