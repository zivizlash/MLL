using MLL.Common.Threading;

namespace MLL.ThreadingOptimization;

public class ThreadedProcessorController
{
    public IThreadedComputer Computer { get; }
    public ITimeTracker TimeTracker { get; }

    public ThreadedProcessorController(IThreadedComputer computer, ITimeTracker timeTracker)
    {
        Computer = computer;
        TimeTracker = timeTracker;
    }
}
