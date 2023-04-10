using MLL.Common.Threading;
using MLL.Common.Tools;

namespace MLL.Computers.Tools;

public struct ForkJoinHelper
{
    public readonly CountdownEvent? Countdown;
    public readonly int ThreadsCount;
    public readonly ProcessingRangeSliced Range;

    public ForkJoinHelper(int threadsCount, ProcessingRange range, CountdownEvent? countdown)
    {
        ThreadsCount = threadsCount;
        Countdown = countdown;
        Range = range.Slice(threadsCount, SliceDistribution.FillRemainderEvenly);
    }

    public ProcessingRange GetProcessingRange(int thread)
    {
        return Range.GetSlice(thread);
    }

    public static ForkJoinHelper Create(LayerThreadInfo threadInfo, int neuronsCount, ProcessingRange range)
    {
        CountdownEvent? countdown = null;
        return Create(threadInfo, neuronsCount, range, ref countdown);
    }

    public static ForkJoinHelper Create(LayerThreadInfo threadInfo, int neuronsCount, 
        ProcessingRange range, ref CountdownEvent? countdown)
    {
        int threadsCount = Math.Min(threadInfo.Threads, range.Length);

        if (threadsCount > 1)
        {
            (countdown ??= new(threadsCount)).Reset(threadsCount);
        }
        else
        {
            countdown?.Dispose();
            countdown = null;
        }

        return new ForkJoinHelper(threadsCount, range, countdown);
    }
}
