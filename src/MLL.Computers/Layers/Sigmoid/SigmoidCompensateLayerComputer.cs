using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Common.Tools;
using MLL.Computers.Layers.Sigmoid.WorkItems;
using MLL.Computers.Tools;

namespace MLL.Computers.Layers.Sigmoid;

public class SigmoidCompensateLayerComputer : ICompensateLayerComputer, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }

    private SigmoidLayerCompensateWorkItem[] _workItems = Array.Empty<SigmoidLayerCompensateWorkItem>();

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
            var generalError = GetGeneralError(learningRate, outputs[ni], errors[ni]);

            for (int wi = 0; wi < weights.Length; wi++)
            {
                weights[wi] += generalError * input[wi];
            }
        }

        fork.Countdown?.Wait();
    }

    private static float GetGeneralError(float learningRate, float output, float error)
    {
        float sigmoidDerivative = NumberTools.SigmoidDerivative(output);
        return learningRate * error * sigmoidDerivative;
    }
}
