using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Common.Tools;
using MLL.Computers.Layers.Sum.WorkItems;
using MLL.Computers.Tools;

namespace MLL.Computers.Layers.Sum;

public class SumCompensateComputer : ICompensateComputer, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }

    private SumCompensateWorkItem[] _workItems = Array.Empty<SumCompensateWorkItem>();

    public void Compensate(LayerWeights layer, float[] input, float learningRate, float[] errors, float[] outputs)
    {
        var neurons = layer.Neurons;

        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));
        Check.LengthEqual(neurons.Length, errors.Length, nameof(errors));
        Check.LengthEqual(neurons.Length, outputs.Length, nameof(outputs));

        var fork = ForkHelper.Create(ThreadInfo, neurons.Length);

        WorkItemsFiller.EnsureCompensateWorkItems(ref _workItems, layer, input, learningRate, errors, outputs, fork);
        ThreadTools.ExecuteOnThreadPool(_workItems, fork.ThreadsCount);

        var (start, _) = ThreadTools.Loop(outputs.Length, fork.ThreadsCount);

        for (int ni = start; ni < neurons.Length; ni++)
        {
            var weights = neurons[ni];
            var generalError = GetGeneralError(learningRate, errors[ni]);

            for (int wi = 0; wi < weights.Length; wi++)
            {
                weights[wi] += generalError * input[wi];
            }
        }

        fork.Countdown?.Wait();
    }

    private static float GetGeneralError(float learningRate, float error)
    {
        return learningRate * error;
    }
}
