using MLL.Common.Layer.Backpropagation;
using MLL.Common.Threading;
using System.Diagnostics;

namespace MLL.Layer.Backpropagation;

public class ErrorBackpropogationTimeTrackerDecorator : IErrorBackpropagation, ITimeTracker
{
    public List<TimeSpan> Timings { get; } = new();

    public IErrorBackpropagation ErrorBackpropagation { get; }

    public ErrorBackpropogationTimeTrackerDecorator(IErrorBackpropagation errorBackprop)
    {
        ErrorBackpropagation = errorBackprop;
    }

    public void ReorganizeErrors(BackpropContext ctx, float[] errors)
    {
        var sw = Stopwatch.StartNew();
        ErrorBackpropagation.ReorganizeErrors(ctx, errors);
        sw.Stop();
        Timings.Add(sw.Elapsed);
    }
}
