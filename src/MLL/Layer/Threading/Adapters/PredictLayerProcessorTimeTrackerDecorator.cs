using MLL.Layer.Computers;
using System.Diagnostics;

namespace MLL.Layer.Threading.Adapters;

public class PredictLayerProcessorTimeTrackerDecorator : IPredictLayerComputer, ITimeTracker
{
    public IPredictLayerComputer Computer { get; }
    public List<TimeSpan> Timings { get; }

    public PredictLayerProcessorTimeTrackerDecorator(IPredictLayerComputer computer)
    {
        Computer = computer;
        Timings = new();
    }

    public void Predict(LayerWeightsData layer, float[] input, float[] results)
    {
        var sw = Stopwatch.StartNew();
        Computer.Predict(layer, input, results);
        sw.Stop();
        Timings.Add(sw.Elapsed);
    }
}
