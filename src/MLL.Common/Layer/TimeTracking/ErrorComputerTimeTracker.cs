using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Common.Tools;
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

    public void CalculateErrors(float[] outputs, float[] expected, float[] errors, ProcessingRange range)
    {
        var sw = Stopwatch.StartNew();
        Computer.CalculateErrors(outputs, expected, errors, range);
        sw.Stop();
        Timings.Add(sw.Elapsed);
    }
}
