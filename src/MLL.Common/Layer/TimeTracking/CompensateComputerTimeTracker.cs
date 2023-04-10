using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Common.Tools;
using System.Diagnostics;

namespace MLL.Common.Layer.TimeTracking;

public class CompensateComputerTimeTracker : ICompensateComputer, ITimeTracker
{
    public ICompensateComputer Computer { get; }
    public List<TimeSpan> Timings { get; }

    public CompensateComputerTimeTracker(ICompensateComputer computer)
    {
        Computer = computer;
        Timings = new();
    }

    public void Compensate(LayerWeights layer, float[] input, float learningRate, 
        float[] errors, float[] outputs, ProcessingRange range)
    {
        var sw = Stopwatch.StartNew();
        Computer.Compensate(layer, input, learningRate, errors, outputs, range);
        sw.Stop();
        Timings.Add(sw.Elapsed);
    }
}
