using MLL.Common.Threading;
using MLL.Common.Tools;

namespace MLL.Computers.Tools;

public struct ForkJoinHelper
{
    public readonly CountdownEvent? Countdown;
    public readonly int ThreadsCount;
    public readonly int ProcessingCount;

    private readonly int _itemsTotal;

    public ForkJoinHelper(int itemsTotal, int threadsCount, int processingCount, CountdownEvent? countdown)
    {
        _itemsTotal = itemsTotal;
        ThreadsCount = threadsCount;
        ProcessingCount = processingCount;
        Countdown = countdown;
    }

    public ProcessingRange GetProcessingRange(int thread)
    {
        if (thread >= ThreadsCount)
        {
            Throw.ArgumentOutOfRange(nameof(thread));
        }

        var start = ProcessingCount * thread;

        var length = thread == ThreadsCount - 1 
            ? ProcessingCount + (_itemsTotal % ThreadsCount) 
            : ProcessingCount;

        return new ProcessingRange(start, start + length);
    }

    public static ForkJoinHelper Create(LayerThreadInfo threadInfo, int neuronsCount)
    {
        CountdownEvent? countdown = null;
        return Create(threadInfo, neuronsCount, ref countdown);
    }

    public static ForkJoinHelper Create(LayerThreadInfo threadInfo, int neuronsCount, ref CountdownEvent? countdown)
    {
        int threadsCount = Math.Min(threadInfo.Threads, neuronsCount);

        if (threadsCount > 1)
        {
            if (countdown == null)
            {
                countdown = new CountdownEvent(threadsCount);
            }
            else
            {
                countdown.Reset(threadsCount);
            }
        }
        else
        {
            countdown?.Dispose();
            countdown = null;
        }

        int processingCount = ThreadTools.Counts(threadInfo.Threads, neuronsCount);
        return new ForkJoinHelper(neuronsCount, threadsCount, processingCount, countdown);
    }
}
