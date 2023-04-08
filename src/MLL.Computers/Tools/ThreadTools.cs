using MLL.Common.Layer;
using MLL.Common.Tools;

namespace MLL.Computers.Tools;

public static class ThreadTools
{
    public static void ExecuteOnThreadPool(IHasExecuteDelegate[] works, CountdownEvent? countdown)
    {
        for (int i = 0; i < works.Length - 1; i++)
        {
            ThreadPool.QueueUserWorkItem(works[i].ExecuteDelegate, null, false);
        }

        works[^1].ExecuteDelegate.Invoke(null);
        countdown?.Wait();
    }

    public static ProcessingRange Loop(int itemsPerThread, int index)
    {
        int start = index * itemsPerThread;
        return new ProcessingRange(start, start + itemsPerThread);
    }

    public static int Counts(int threadsCount, int itemsCount)
    {
        if (threadsCount == 0)
        {
            return 0;
        }

        return itemsCount / threadsCount;
    }
}
