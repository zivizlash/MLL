using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using System.Diagnostics;

namespace MLL.Common.Layer.TimeTracking;

public class PredictComputerTimeTracker : IPredictComputer, ITimeTracker
{
    public IPredictComputer Computer { get; }
    public List<TimeSpan> Timings { get; }

    public PredictComputerTimeTracker(IPredictComputer computer)
    {
        Computer = computer;
        Timings = new();
    }

    public void Predict(LayerWeights layer, float[] input, float[] results)
    {
        var sw = Stopwatch.StartNew();
        Computer.Predict(layer, input, results);
        sw.Stop();
        Timings.Add(sw.Elapsed);
    }
}
