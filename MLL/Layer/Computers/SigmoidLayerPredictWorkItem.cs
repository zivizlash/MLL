using MLL.Tools;

namespace MLL.Layer.Computers;

public class SigmoidLayerPredictWorkItem : IHasExecuteDelegate
{
    public LayerWeightsData Layer;
    public float[] Input;
    public float[] Results;
    public int ProcessingCount;
    public int Index;
    public CountdownEvent Countdown;
    public SigmoidLayerPredictWorkItem[] WorkItems;

    public WaitCallback ExecuteDelegate { get; }

    public SigmoidLayerPredictWorkItem()
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
            Results[ni] = NumberTools.Sigmoid(sum);
        }

        Countdown.Signal();
    }
}
