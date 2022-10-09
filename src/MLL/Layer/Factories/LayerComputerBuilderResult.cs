using MLL.Layer.Computers.Sigmoid;
using MLL.Layer.Threading;

namespace MLL.Layer.Factories;

public class LayerComputerBuilderResult
{
    public IReadOnlyList<LayerComputers> Computers { get; }
    public IReadOnlyList<ThreadedProcessorStatCollector> Collectors { get; }

    public LayerComputerBuilderResult(IReadOnlyList<LayerComputers> computers,
        IReadOnlyList<ThreadedProcessorStatCollector> collectors)
    {
        Computers = computers;
        Collectors = collectors;
    }
}
