using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Common.Tools;
using MLL.Computers.Layers.Common.WorkInfo;
using MLL.Computers.Tools;

namespace MLL.Computers.Layers.Sigmoid;

public class SigmoidCompensateComputer : ICompensateComputer, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }

    private SigmoidCompensateWorkItem[] _workItems = Array.Empty<SigmoidCompensateWorkItem>();

    public SigmoidCompensateComputer()
    {
        ThreadInfo = new LayerThreadInfo(1);
    }

    public void Compensate(LayerWeights layer, float[] input, float learningRate, 
        float[] errors, float[] outputs, ProcessingRange range)
    {
        var neurons = layer.Weights;

        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));
        Check.LengthEqual(neurons.Length, errors.Length, nameof(errors));
        Check.LengthEqual(neurons.Length, outputs.Length, nameof(outputs));
        Check.WithinRange(layer.Weights, range, nameof(layer));

        var fork = ForkJoinHelper.Create(ThreadInfo, neurons.Length, range);

        WorkItemsFiller.EnsureCompensateWorkItems(ref _workItems, layer, input, learningRate, errors, outputs, fork);
        ThreadTools.ExecuteOnThreadPool(_workItems, fork.Countdown);
    }

    public class SigmoidCompensateWorkItem : IHasExecuteDelegate, IHasCompensateWorkInfo
    {
        public CompensateWorkInfo WorkInfo { get; set; }
        public Action<object?> ExecuteDelegate { get; }

        public SigmoidCompensateWorkItem()
        {
            ExecuteDelegate = Execute;
        }

        public void Execute(object? _)
        {
            var neurons = WorkInfo.Layer.Weights;
            var (start, stop) = WorkInfo.ProcessingRange;

            for (int ni = start; ni < stop; ni++)
            {
                var weights = neurons[ni];
                var generalError = GetGeneralError(WorkInfo.LearningRate, WorkInfo.Outputs[ni], WorkInfo.Errors[ni]);

                for (int wi = 0; wi < weights.Length; wi++)
                {
                    weights[wi] += generalError * WorkInfo.Input[wi];
                }
            }

            WorkInfo.Countdown?.Signal();
        }

        private static float GetGeneralError(float learningRate, float output, float error)
        {
            float sigmoidDerivative = NumberTools.SigmoidDerivative(output);
            return learningRate * error * sigmoidDerivative;
        }
    }
}
