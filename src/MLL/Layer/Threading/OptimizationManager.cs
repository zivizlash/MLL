namespace MLL.Layer.Threading;

public class OptimizationManager
{
    private readonly HashSet<ThreadedProcessorStatCollector> _collectors;
    
    public OptimizationManager(params ThreadedProcessorStatCollector[] collectors)
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
                Console.WriteLine($"Module {collector.Controller.TimeTracker.GetType()} optimized");
            }
        }

        return _collectors.Count == 0;
    }
}
