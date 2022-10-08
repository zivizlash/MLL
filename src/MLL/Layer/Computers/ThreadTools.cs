namespace MLL.Layer.Computers;

public static class ThreadTools
{
    public static void ExecuteOnThreadPool(IHasExecuteDelegate[] hasDelegates, int threadsCount)
    {
        if (threadsCount > 0)
        {
            for (int i = 0; i < hasDelegates.Length; i++)
            {
                ThreadPool.QueueUserWorkItem(hasDelegates[i].ExecuteDelegate);
            }
        }
    }

    public static (int start, int end) Loop(int itemsPerThread, int index)
    {
        int start = index * itemsPerThread;
        return (start, start + itemsPerThread);
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
