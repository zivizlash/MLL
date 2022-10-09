using MLL.Common.Layer;
using MLL.Layer.Computers;
using MLL.Tools;

namespace MLL.Computers.Layers.Sum.WorkItems;

public class SumLayerPredictWorkItem : IHasExecuteDelegate
{
    public LayerWeights Layer;
    public float[] Input;
    public float[] Results;
    public int ProcessingCount;
    public int Index;
    public CountdownEvent Countdown;

    public WaitCallback ExecuteDelegate { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public SumLayerPredictWorkItem()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
