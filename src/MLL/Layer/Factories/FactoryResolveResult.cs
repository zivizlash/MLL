using MLL.Layer.Computers.Sigmoid;
using MLL.Layer.Threading;

namespace MLL.Layer.Factories;

public struct FactoryResolveResult
{
    public ThreadedProcessorStatCollector[] Collectors;
    public NeuronComputers NeuronComputers;
}
