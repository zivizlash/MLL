using MLL.Common.Threading;

namespace MLL.Layer.Computers.Sigmoid;

public struct ForkHelper
{
    public CountdownEvent? Countdown;
    public int ThreadsCount;
    public int ProcessingCount;

    public static ForkHelper Create(LayerThreadInfo threadInfo, int neuronsCount)
    {
        int threadsCount = Math.Min(threadInfo.Threads - 1, neuronsCount);
        var countdown = threadsCount > 0 ? new CountdownEvent(threadsCount) : null;
        int processingCount = ThreadTools.Counts(threadInfo.Threads, neuronsCount);

        return new ForkHelper
        {
            ThreadsCount = threadsCount,
            ProcessingCount = processingCount,
            Countdown = countdown
        };
    }
}
