using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Common.Tools;
using MLL.Computers.Layers.Common.WorkInfo;
using MLL.Computers.Tools;

namespace MLL.Computers.Layers.Sum;

public class SumPredictComputer : IPredictComputer, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }
    private SumPredictWorkItem[] _workItems = Array.Empty<SumPredictWorkItem>();

    public SumPredictComputer()
    {
        ThreadInfo = new LayerThreadInfo(1);
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

    private class SumPredictWorkItem : IHasExecuteDelegate, IHasPredictWorkInfo
    {
        public PredictWorkInfo WorkInfo { get; set; }
        public Action<object?> ExecuteDelegate { get; }

        public SumPredictWorkItem()
        {
            ExecuteDelegate = Execute;
        }

        public void Execute(object? _)
        {
            var neurons = WorkInfo.Layer.Weights;
            var (start, end) = WorkInfo.ProcessingRange;

            for (int ni = start; ni < end; ni++)
            {
                var sum = VectorCalculator.CalculateMultiplySum(neurons[ni], WorkInfo.Input);
                WorkInfo.Results[ni] = sum;
            }

            WorkInfo.Countdown?.Signal();
        }
    }
}
