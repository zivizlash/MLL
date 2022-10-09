using MLL.Common.Layer;
using MLL.Common.Threading;

namespace MLL.Common.Optimization;

public readonly struct OptimizatorFactoryParams
{
    public readonly IThreadedComputer ThreadedComputer;
    public readonly LayerComputers LayerComputers;

    public OptimizatorFactoryParams(IThreadedComputer threadedComputer, LayerComputers layerComputers)
    {
        ThreadedComputer = threadedComputer;
        LayerComputers = layerComputers;
    }
}
