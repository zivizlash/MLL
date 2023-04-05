using MLL.Common.Layer;
using MLL.Computers.Layers.Common.WorkInfo;

namespace MLL.Computers.Layers.Common.WorkItems;

public class CommonErrorCalcWorkItem : IHasExecuteDelegate, IHasErrorWorkInfo
{
    public ErrorWorkInfo WorkInfo { get; set; }
    public Action<object?> ExecuteDelegate { get; }

    public CommonErrorCalcWorkItem()
    {
        ExecuteDelegate = Execute;
    }

    public void Execute(object? _)
    {
        var (start, stop) = WorkInfo.ProcessingRange;

        for (int neuronIndex = start; neuronIndex < stop; neuronIndex++)
        {
            WorkInfo.Errors[neuronIndex] = WorkInfo.Expected[neuronIndex] - WorkInfo.Outputs[neuronIndex];
        }

        WorkInfo.Countdown?.Signal();
    }
}
