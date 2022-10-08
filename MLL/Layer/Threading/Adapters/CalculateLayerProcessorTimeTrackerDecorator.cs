using MLL.Layer.Computers;
using System.Diagnostics;

namespace MLL.Layer.Threading.Adapters;

public class CalculateLayerProcessorTimeTrackerDecorator : ICalculateLayerComputer, ITimeTracker
{
    public ICalculateLayerComputer Computer { get; }
    public List<TimeSpan> Timings { get; }

    public CalculateLayerProcessorTimeTrackerDecorator(ICalculateLayerComputer computer)
    {
        Computer = computer;
        Timings = new();
    }

    public void CalculateErrors(float[] outputs, float[] expected, float[] errors)
    {
        var sw = Stopwatch.StartNew();
        Computer.CalculateErrors(outputs, expected, errors);
        sw.Stop();
        Timings.Add(sw.Elapsed);
    }
}
