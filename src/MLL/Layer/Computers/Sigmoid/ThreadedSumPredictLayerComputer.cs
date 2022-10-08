using MLL.Layer.Threading;
using MLL.Tools;

namespace MLL.Layer.Computers.Sigmoid;

public class ThreadedSumPredictLayerComputer : IPredictLayerComputer, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }

    private SumLayerPredictWorkItem[] _workItems = Array.Empty<SumLayerPredictWorkItem>();

    public void Predict(LayerWeightsData layer, float[] input, float[] results)
    {
        var neurons = layer.Neurons;

        Check.LengthEqual(neurons.Length, results.Length, nameof(results));
        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));

        var fork = ForkHelper.Create(ThreadInfo, neurons.Length);

        WorkItems.EnsurePredictWorkItems(ref _workItems, layer, input, results, fork);
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
