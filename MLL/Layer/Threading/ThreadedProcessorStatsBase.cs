namespace MLL.Layer.Threading;

//public abstract class ThreadedProcessorStatsBase : IThreadedProcessorStats
//{
//    private readonly List<TimeSpan> _trackedTime;
//    private int _threadsCount;

//    public IReadOnlyList<TimeSpan> TrackedTime => _trackedTime;

//    public int ThreadsCount
//    {
//        get => _threadsCount;
//        set
//        {
//            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value));
//            _threadsCount = value;
//        }
//    }

//    protected ThreadedProcessorStatsBase() => _trackedTime = new();
//    protected void AddTrackedTime(TimeSpan time) => _trackedTime.Add(time);
//    public void ClearTrackedTime() => _trackedTime.Clear();
//}
