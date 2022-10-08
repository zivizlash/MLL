namespace MLL.Layer.Threading;

public readonly struct LayerThreadInfo
{
    public readonly int Threads;

    public LayerThreadInfo(int threads)
    {
        Threads = threads;
    }

    public static bool operator ==(LayerThreadInfo left, LayerThreadInfo right)
    {
        return left.Threads == right.Threads;
    }

    public static bool operator !=(LayerThreadInfo left, LayerThreadInfo right)
    {
        return left.Threads != right.Threads;
    }
}
