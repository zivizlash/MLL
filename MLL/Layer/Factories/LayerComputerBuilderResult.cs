using MLL.Layer.Computers.Sigmoid;
using MLL.Layer.Threading;

namespace MLL.Layer.Factories;

public class LayerComputerBuilderResult
{
    public IReadOnlyList<NeuronComputers> Computers { get; }
    public IReadOnlyList<ThreadedProcessorStatCollector> Collectors { get; }

    public LayerComputerBuilderResult(IReadOnlyList<NeuronComputers> computers,
        IReadOnlyList<ThreadedProcessorStatCollector> collectors)
    {
        Computers = computers;
        Collectors = collectors;
    }
}
