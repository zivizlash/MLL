namespace MLL.Common.Threading;

public interface ITimeTracker
{
    List<TimeSpan> Timings { get; }
}
