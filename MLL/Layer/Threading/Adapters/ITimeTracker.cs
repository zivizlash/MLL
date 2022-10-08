namespace MLL.Layer.Threading.Adapters;

public interface ITimeTracker
{
    List<TimeSpan> Timings { get; }
}
