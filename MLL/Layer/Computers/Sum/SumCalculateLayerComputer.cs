using MLL.Layer.Computers.Sigmoid;
using MLL.Layer.Threading;
using MLL.Tools;

namespace MLL.Layer.Computers.Sum;

public class SumCalculateLayerComputer : ICalculateLayerComputer
{
    public void CalculateErrors(float[] outputs, float[] expected, float[] errors)
    {
        Check.LengthEqual(outputs.Length, errors.Length, nameof(errors));
        Check.LengthEqual(outputs.Length, expected.Length, nameof(expected));

        for (int ri = 0; ri < outputs.Length; ri++)
        {
            errors[ri] = expected[ri] - outputs[ri];
        }
    }
}

public class ThreadedSumCalculateLayerComputer : ICalculateLayerComputer, IThreadedComputer
{
    private LayerErrorCalcWorkItem[] _items = Array.Empty<LayerErrorCalcWorkItem>();

    public LayerThreadInfo ThreadInfo { get; set; }

    public void CalculateErrors(float[] outputs, float[] expected, float[] errors)
    {
        Check.LengthEqual(outputs.Length, errors.Length, nameof(errors));
        Check.LengthEqual(outputs.Length, expected.Length, nameof(expected));

        var fork = ForkHelper.Create(ThreadInfo, outputs.Length);

        ThreadTools.EnsureCalculateWorkItems(ref _items, outputs, expected, errors, fork);
        ThreadTools.ExecuteOnThreadPool(_items, fork.ThreadsCount);

        var (start, _) = ThreadTools.Loop(fork.ProcessingCount, fork.ThreadsCount);

        for (int neuronIndex = start; neuronIndex < outputs.Length; neuronIndex++)
        {
            errors[neuronIndex] = expected[neuronIndex] - outputs[neuronIndex];
        }

        fork.Countdown?.Wait();
    }
}
