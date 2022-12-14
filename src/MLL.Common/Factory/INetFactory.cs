namespace MLL.Common.Factory;

using MLL.Common.Builders.Computers;
using MLL.Common.Builders.Weights;
using MLL.Common.Optimization;
using MLL.Common.Tools;
using Net;

public abstract class RandomFillNetFactory : NetFactory
{
    private readonly int _randomSeed;

    protected RandomFillNetFactory(int randomSeed)
    {
        _randomSeed = randomSeed;
    }

    public override void PostSetup(Net net)
    {
        net.Weights.Layers.RandomFill(_randomSeed);
        base.PostSetup(net);
    }
}

public abstract class NetFactory : INetFactory
{
    public abstract LayerWeightsDefinition[] GetDefinitions();
    public abstract LayerComputerBuilderResult GetComputers(bool isForTrain);
    
    public virtual void PostSetup(Net net)
    {
    }

    public virtual Net Create(bool isForTrain)
    {
        var weights = GetDefinitions().ToWeights();
        var computers = GetComputers(isForTrain);

        var optimizator = new OptimizationManager(computers.Collectors);
        var buffer = NetLayersBuffers.CreateByWeights(weights);

        var net = new Net(computers.Computers, weights, optimizator, buffer);
        PostSetup(net);

        return net;
    }
}

public interface INetFactory
{
    Net Create(bool isForTrain);
}
