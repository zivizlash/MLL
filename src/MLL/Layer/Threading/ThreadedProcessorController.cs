using MLL.Common.Threading;

namespace MLL.Layer.Threading;

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
