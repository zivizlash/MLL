using MLL.Common.Layer.Backpropagation;
using System.Diagnostics;

namespace MLL.Layer.Backpropagation;

public class TimeErrorBackpropagationDecorator : IErrorBackpropagation
{
    public IErrorBackpropagation ErrorBackprop { get; set; }
    public List<TimeSpan> TrackedTime { get; }

    public TimeErrorBackpropagationDecorator(IErrorBackpropagation errorBackprop)
    {
        ErrorBackprop = errorBackprop;
        TrackedTime = new List<TimeSpan>();
    }

    public void ReorganizeErrors(BackpropContext ctx, float[] errors)
    {
        var stopwatch = Stopwatch.StartNew();

        ErrorBackprop.ReorganizeErrors(ctx, errors);

        stopwatch.Stop();
        TrackedTime.Add(stopwatch.Elapsed);
    }
}
