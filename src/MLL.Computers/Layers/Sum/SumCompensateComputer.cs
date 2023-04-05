using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Common.Tools;
using MLL.Computers.Layers.Common.WorkInfo;
using MLL.Computers.Tools;

namespace MLL.Computers.Layers.Sum;

public class SumCompensateComputer : ICompensateComputer, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }

    private SumCompensateWorkItem[] _workItems = Array.Empty<SumCompensateWorkItem>();

    public SumCompensateComputer()
    {
        ThreadInfo = new(1);
    }

    public void Compensate(LayerWeights layer, float[] input, float learningRate, float[] errors, float[] outputs)
    {
        var neurons = layer.Weights;

        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));
        Check.LengthEqual(neurons.Length, errors.Length, nameof(errors));
        Check.LengthEqual(neurons.Length, outputs.Length, nameof(outputs));

        var fork = ForkJoinHelper.Create(ThreadInfo, neurons.Length);
        WorkItemsFiller.EnsureCompensateWorkItems(ref _workItems, layer, input, learningRate, errors, outputs, fork);
        ThreadTools.ExecuteOnThreadPool(_workItems, fork.Countdown);
    }

    private class SumCompensateWorkItem : IHasExecuteDelegate, IHasCompensateWorkInfo
    {
        public CompensateWorkInfo WorkInfo { get; set; }
        public Action<object?> ExecuteDelegate { get; }

        public SumCompensateWorkItem()
        {
            ExecuteDelegate = Execute;
        }

        public void Execute(object? _)
        {
            var (start, end) = WorkInfo.ProcessingRange;

            var neurons = WorkInfo.Layer.Weights;

            for (int ni = start; ni < end; ni++)
            {
                var weights = neurons[ni];
                var generalError = GetGeneralError(WorkInfo.LearningRate, WorkInfo.Errors[ni]);

                for (int wi = 0; wi < weights.Length; wi++)
                {
                    weights[wi] += generalError * WorkInfo.Input[wi];
                }
            }

            WorkInfo.Countdown?.Signal();
        }

        private static float GetGeneralError(float learningRate, float error)
        {
            return learningRate * error;
        }
    }
}
