using MLL.Layer.Computers.Sigmoid;
using MLL.Layer.Threading.Adapters;

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
