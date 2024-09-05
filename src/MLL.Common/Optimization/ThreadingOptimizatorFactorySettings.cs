namespace MLL.Common.Optimization;

public readonly struct ThreadingOptimizatorFactorySettings
{
    public readonly int RequiredSamples;
    public readonly float OutlinersThreshold;
    public readonly int MaxThreads;

    public ThreadingOptimizatorFactorySettings(int requiredSamples, float outlinersThreshold, int maxThreads)
    {
        RequiredSamples = requiredSamples;
        OutlinersThreshold = outlinersThreshold;
        MaxThreads = maxThreads;
    }
}
