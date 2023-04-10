using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Common.Tools;
using MLL.Computers.Layers.Common.WorkInfo;
using MLL.Computers.Tools;

namespace MLL.Computers.Layers.Sum;

public class CommonErrorComputer : IErrorComputer, IThreadedComputer
{
    private CommonErrorCalcWorkItem[] _items = Array.Empty<CommonErrorCalcWorkItem>();

    public LayerThreadInfo ThreadInfo { get; set; }

    public CommonErrorComputer()
    {
        ThreadInfo = new LayerThreadInfo(1);
    }

    public void CalculateErrors(float[] outputs, float[] expected, float[] errors, ProcessingRange range)
    {
        Check.LengthEqual(outputs.Length, errors.Length, nameof(errors));
        Check.LengthEqual(outputs.Length, expected.Length, nameof(expected));
        Check.WithinRange(errors, range, nameof(errors));

        var fork = ForkJoinHelper.Create(ThreadInfo, outputs.Length, range);

        WorkItemsFiller.EnsureCalculateWorkItems(ref _items, outputs, expected, errors, fork);
        ThreadTools.ExecuteOnThreadPool(_items, fork.Countdown);
    }

    private class CommonErrorCalcWorkItem : IHasExecuteDelegate, IHasErrorWorkInfo
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
}
