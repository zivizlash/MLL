using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Common.Tools;
using MLL.Computers.Layers.Sum.WorkItems;
using MLL.Layer.Computers;
using MLL.Layer.Computers.Sigmoid;

namespace MLL.Computers.Layers.Sum;

public class SumCalculateLayerComputer : ICalculateLayerComputer, IThreadedComputer
{
    private SumLayerErrorCalcWorkItem[] _items = Array.Empty<SumLayerErrorCalcWorkItem>();

    public LayerThreadInfo ThreadInfo { get; set; }

    public void CalculateErrors(float[] outputs, float[] expected, float[] errors)
    {
        Check.LengthEqual(outputs.Length, errors.Length, nameof(errors));
        Check.LengthEqual(outputs.Length, expected.Length, nameof(expected));

        var fork = ForkHelper.Create(ThreadInfo, outputs.Length);

        WorkItemsFiller.EnsureCalculateWorkItems(ref _items, outputs, expected, errors, fork);
        ThreadTools.ExecuteOnThreadPool(_items, fork.ThreadsCount);

        var (start, _) = ThreadTools.Loop(fork.ProcessingCount, fork.ThreadsCount);

        for (int neuronIndex = start; neuronIndex < outputs.Length; neuronIndex++)
        {
            errors[neuronIndex] = expected[neuronIndex] - outputs[neuronIndex];
        }

        fork.Countdown?.Wait();
    }
}
