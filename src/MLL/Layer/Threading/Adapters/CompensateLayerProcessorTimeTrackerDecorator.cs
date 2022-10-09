using MLL.Layer.Computers;
using System.Diagnostics;

namespace MLL.Layer.Threading.Adapters;

public class CompensateLayerProcessorTimeTrackerDecorator : ICompensateLayerComputer, ITimeTracker
{
    public ICompensateLayerComputer Computer { get; }
    public List<TimeSpan> Timings { get; }

    public CompensateLayerProcessorTimeTrackerDecorator(ICompensateLayerComputer computer)
    {
        Computer = computer;
        Timings = new();
    }

    public void Compensate(LayerWeights layer, float[] input, float learningRate, float[] errors, float[] outputs)
    {
        var sw = Stopwatch.StartNew();
        Computer.Compensate(layer, input, learningRate, errors, outputs);
        sw.Stop();
        Timings.Add(sw.Elapsed);
    }
}
