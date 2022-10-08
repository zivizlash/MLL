using MLL.Layer.Threading;

namespace MLL.Layer.Computers.Sigmoid;

public interface IThreadedComputer
{
    LayerThreadInfo ThreadInfo { get; set; }
}
