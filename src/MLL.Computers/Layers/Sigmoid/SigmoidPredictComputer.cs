using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Common.Tools;
using MLL.Computers.Layers.Sigmoid.WorkItems;
using MLL.Computers.Tools;

namespace MLL.Computers.Layers.Sigmoid;

public class SigmoidPredictComputer : IPredictComputer, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }

    private SigmoidPredictWorkItem[] _workItems = Array.Empty<SigmoidPredictWorkItem>();

    public void Predict(LayerWeights layer, float[] input, float[] results)
    {
        var neurons = layer.Weights;

        Check.LengthEqual(neurons.Length, results.Length, nameof(results));
        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));

        var fork = ForkHelper.Create(ThreadInfo, neurons.Length);

        WorkItemsFiller.EnsurePredictWorkItems(ref _workItems, layer, input, results, fork);
        ThreadTools.ExecuteOnThreadPool(_workItems, fork.ThreadsCount);

        var (start, _) = ThreadTools.Loop(fork.ProcessingCount, fork.ThreadsCount);

        for (int ni = start; ni < neurons.Length; ni++)
        {
            var sum = VectorCalculator.CalculateMultiplySum(neurons[ni], input);
            results[ni] = NumberTools.Sigmoid(sum);
        }

        fork.Countdown?.Wait();
    }
}
