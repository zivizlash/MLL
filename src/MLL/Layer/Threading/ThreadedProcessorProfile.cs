namespace MLL.Layer.Threading;

public class ThreadedProcessorProfile
{
    private readonly int _maxThreads;

    public Dictionary<int, TimeSpan> Stats { get; } = new();
    public int CurrentOptimal { get; private set; }

    public ThreadedProcessorProfile(int maxThreads)
    {
        _maxThreads = maxThreads;
    }

    public bool IsFoundOptimal()
    {
        if (Stats.Count == 0) return false;

        var (minIndex, _) = FindMin();
        CurrentOptimal = minIndex;

        return IsCheckedAndGreater(minIndex, -1) && IsCheckedAndGreater(minIndex, 1);
    }

    private (int, TimeSpan) FindMin()
    {
        var min = Stats.MinBy(kv => kv.Value);
        return (min.Key, min.Value);
    }

    private bool IsNeedCheck(int originalIndex, int offset)
    {
        if (originalIndex <= 1) return false;
        if (originalIndex >= _maxThreads) return false;

        return Stats.ContainsKey(originalIndex + offset);
    }

    private bool IsGreaterThenTime(int originalIndex, int offset)
    {
        var index = originalIndex + offset;

        if (index >= _maxThreads) return false;
        if (index < 1) return false;

        return Stats[originalIndex] > Stats[index];
    }

    private bool IsCheckedAndGreater(int originalIndex, int offset)
    {
        return !IsNeedCheck(originalIndex, offset) && IsGreaterThenTime(originalIndex, offset);
    }
}
