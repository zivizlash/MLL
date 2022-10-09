using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Common.Tools;
using MLL.Computers.Layers.Sum.WorkItems;
using MLL.Layer.Computers;
using MLL.Layer.Computers.Sigmoid;
using MLL.Tools;

namespace MLL.Computers.Layers.Sum;

public class SumPredictLayerComputer : IPredictLayerComputer, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }

    private SumLayerPredictWorkItem[] _workItems = Array.Empty<SumLayerPredictWorkItem>();

    public void Predict(LayerWeights layer, float[] input, float[] results)
    {
        var neurons = layer.Neurons;

        Check.LengthEqual(neurons.Length, results.Length, nameof(results));
        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));

        var fork = ForkHelper.Create(ThreadInfo, neurons.Length);

        WorkItemsFiller.EnsurePredictWorkItems(ref _workItems, layer, input, results, fork);
        ThreadTools.ExecuteOnThreadPool(_workItems, fork.ThreadsCount);

        var (start, _) = ThreadTools.Loop(fork.ProcessingCount, fork.ThreadsCount);

        for (int ni = start; ni < neurons.Length; ni++)
        {
            var sum = VectorCalculator.CalculateMultiplySum(neurons[ni], input);
            results[ni] = sum;
        }

        fork.Countdown?.Wait();
    }
}
