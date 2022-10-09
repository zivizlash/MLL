using MLL.Common.Layer;
using MLL.Computers.Tools;

namespace MLL.Computers.Layers.Sum.WorkItems;

public class SumErrorCalcWorkItem : IHasExecuteDelegate
{
    public float[] Outputs;
    public float[] Expected;
    public float[] Errors;
    public int ProcessingCount;
    public int Index;
    public CountdownEvent Countdown;

    public WaitCallback ExecuteDelegate { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public SumErrorCalcWorkItem()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        ExecuteDelegate = Execute;
    }

    public void Execute(object? _)
    {
        var (start, end) = ThreadTools.Loop(ProcessingCount, Index);

        for (int neuronIndex = start; neuronIndex < end; neuronIndex++)
        {
            Errors[neuronIndex] = Expected[neuronIndex] - Outputs[neuronIndex];
        }

        Countdown.Signal();
    }
}
