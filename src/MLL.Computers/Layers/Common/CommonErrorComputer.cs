using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Common.Tools;
using MLL.Computers.Layers.Common.WorkItems;
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

    public void CalculateErrors(float[] outputs, float[] expected, float[] errors)
    {
        Check.LengthEqual(outputs.Length, errors.Length, nameof(errors));
        Check.LengthEqual(outputs.Length, expected.Length, nameof(expected));

        var fork = ForkJoinHelper.Create(ThreadInfo, outputs.Length);
        WorkItemsFiller.EnsureCalculateWorkItems(ref _items, outputs, expected, errors, fork);
        ThreadTools.ExecuteOnThreadPool(_items, fork.Countdown);
    }
}
