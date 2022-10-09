using MLL.Common.Optimization;
using MLL.Tools;

namespace MLL.Layer.Threading;

public class ThreadedProcessorStatCollector : IOptimizator
{
    private readonly int _requiredSamples;
    private readonly float _outlinersThreshold;
    private readonly ThreadedProcessorProfile _profile;
    private readonly Action _optimizeDoneAction;

    private ListSelection<int> _threads;

    public ThreadedProcessorController Controller { get; }

    public ThreadedProcessorStatCollector(ThreadedProcessorController controller,
        int requiredTimingsCount, float outlinersThreshold,
        int maxThreads, ListSelection<int> threads, Action optimizeDoneAction)
    {
        _profile = new ThreadedProcessorProfile(maxThreads);
        _requiredSamples = requiredTimingsCount;
        _outlinersThreshold = outlinersThreshold;
        _threads = threads;
        _optimizeDoneAction = optimizeDoneAction;
        Controller = controller;
        Controller.Computer.ThreadInfo = new(_threads.GetNext());
    }

    private bool CalculateAndAddStats()
    {
        var timeTracker = Controller.TimeTracker;

        var rawAverage = FindAverage(timeTracker.Timings);
        var timings = FilterOutliners(timeTracker.Timings, rawAverage.TotalMilliseconds).ToList();

        var timingsCount = timeTracker.Timings.Count;
        timeTracker.Timings.Clear();

        if (timings.Count < timingsCount / 2)
        {
            Console.WriteLine("Sequence contains less then half of source count; "
                + $"Source: {timingsCount}; Actual: {timings.Count}");
            return false;
        }

        _profile.Stats.Add(Controller.Computer.ThreadInfo.Threads, FindAverage(timings));
        return true;
    }

    public bool Optimize()
    {
        if (Controller.TimeTracker.Timings.Count < _requiredSamples)
        {
            return false;
        }

        if (!CalculateAndAddStats())
        {
            return false;
        }

        if (_profile.IsFoundOptimal())
        {
            Console.WriteLine("Found optimal");
            _optimizeDoneAction.Invoke();
            return true;
        }

        if (_threads.TryGetNext(out var threadsCount))
        {
            Controller.Computer.ThreadInfo = new(threadsCount);
            return false;
        }
        else
        {
            Console.WriteLine("Found optimal");
            Controller.Computer.ThreadInfo = new(_profile.CurrentOptimal);
            _optimizeDoneAction.Invoke();
            return true;
        }
    }

    private IEnumerable<TimeSpan> FilterOutliners(IEnumerable<TimeSpan> source, double average)
    {
        return source.Where(tt => Math.Abs(tt.TotalMilliseconds / average - 1.0) <= _outlinersThreshold);
    }

    private static TimeSpan FindAverage(IEnumerable<TimeSpan> times)
    {
        var sorted = times.OrderBy(t => t).ToList();
        return sorted[sorted.Count / 2];
    }
}
