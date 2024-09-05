namespace MLL.Common.Optimization;

public class OptimizationManager
{
    private readonly HashSet<IOptimizator> _collectors;
    
    public OptimizationManager(params IOptimizator[] collectors)
    {
        _collectors = collectors.ToHashSet();
    }

    public OptimizationManager(IEnumerable<IOptimizator> collectors)
    {
        _collectors = collectors.ToHashSet();
    }

    public bool Optimize()
    {
        foreach (var collector in _collectors)
        {
            if (collector.Optimize())
            {
                _collectors.Remove(collector);
                Console.WriteLine($"Module optimized");
            }
        }

        return _collectors.Count == 0;
    }
}
