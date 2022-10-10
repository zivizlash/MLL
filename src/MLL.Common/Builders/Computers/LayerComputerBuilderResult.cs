using MLL.Common.Layer;
using MLL.Common.Optimization;

namespace MLL.Common.Builders.Computers;

public class LayerComputerBuilderResult
{
    public IReadOnlyList<LayerComputers> Computers { get; }
    public IReadOnlyList<IOptimizator> Collectors { get; }

    public LayerComputerBuilderResult(IReadOnlyList<LayerComputers> computers, IReadOnlyList<IOptimizator> collectors)
    {
        Computers = computers;
        Collectors = collectors;
    }
}
