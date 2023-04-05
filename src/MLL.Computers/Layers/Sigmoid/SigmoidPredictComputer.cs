using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Common.Tools;
using MLL.Computers.Layers.Common.WorkInfo;
using MLL.Computers.Tools;

namespace MLL.Computers.Layers.Sigmoid;

public class SigmoidPredictComputer : IPredictComputer, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }

    private SigmoidPredictWorkItem[] _workItems = Array.Empty<SigmoidPredictWorkItem>();

    public SigmoidPredictComputer()
    {
        ThreadInfo = new(1);
    }

    public void Predict(LayerWeights layer, float[] input, float[] results)
    {
        var neurons = layer.Weights;

        Check.LengthEqual(neurons.Length, results.Length, nameof(results));
        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));

        var fork = ForkJoinHelper.Create(ThreadInfo, neurons.Length);
        WorkItemsFiller.EnsurePredictWorkItems(ref _workItems, layer, input, results, fork);
        ThreadTools.ExecuteOnThreadPool(_workItems, fork.Countdown);
    }

    private class SigmoidPredictWorkItem : IHasExecuteDelegate, IHasPredictWorkInfo
    {
        public PredictWorkInfo WorkInfo { get; set; }
        public Action<object?> ExecuteDelegate { get; }

        public SigmoidPredictWorkItem()
        {
            ExecuteDelegate = Execute;
        }

        public void Execute(object? _)
        {
            var neurons = WorkInfo.Layer.Weights;
            var (start, stop) = WorkInfo.ProcessingRange;

            for (int ni = start; ni < stop; ni++)
            {
                var sum = VectorCalculator.CalculateMultiplySum(neurons[ni], WorkInfo.Input);
                WorkInfo.Results[ni] = NumberTools.Sigmoid(sum);
            }

            WorkInfo.Countdown?.Signal();
        }
    }
}
