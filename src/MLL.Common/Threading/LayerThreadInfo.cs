namespace MLL.Common.Threading;

public readonly struct LayerThreadInfo
{
    public readonly int Threads;

    public LayerThreadInfo()
    {
        Threads = 1; 
    }

    public LayerThreadInfo(int threads)
    {
        Threads = threads;
    }
}
