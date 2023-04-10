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
}
