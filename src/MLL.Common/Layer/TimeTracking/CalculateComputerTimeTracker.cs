using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using System.Diagnostics;

namespace MLL.Common.Layer.TimeTracking;

public class CalculateComputerTimeTracker : ICalculateComputer, ITimeTracker
{
    public ICalculateComputer Computer { get; }
    public List<TimeSpan> Timings { get; }

    public CalculateComputerTimeTracker(ICalculateComputer computer)
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
