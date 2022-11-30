using MLL.Common.Layer;
using MLL.Common.Optimization;

namespace MLL.Common.Factory;

public struct FactoryResolveResult
{
    public IOptimizator[] Optimizers;
    public LayerComputers Computers;
}
