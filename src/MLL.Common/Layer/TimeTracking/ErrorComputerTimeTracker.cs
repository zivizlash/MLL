using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using System.Diagnostics;

namespace MLL.Common.Layer.TimeTracking;

public class ErrorComputerTimeTracker : IErrorComputer, ITimeTracker
{
    public IErrorComputer Computer { get; }
    public List<TimeSpan> Timings { get; }

    public ErrorComputerTimeTracker(IErrorComputer computer)
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
