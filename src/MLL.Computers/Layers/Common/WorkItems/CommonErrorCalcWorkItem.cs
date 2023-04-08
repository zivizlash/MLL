using MLL.Common.Layer;
using MLL.Computers.Layers.Common.WorkInfo;
using MLL.Computers.Tools;

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
        VectorCalculator.Substract(WorkInfo.Expected, WorkInfo.Outputs, WorkInfo.Errors, start, stop);
        WorkInfo.Countdown?.Signal();
    }
}
