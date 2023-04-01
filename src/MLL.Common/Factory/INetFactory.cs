namespace MLL.Common.Factory;

using MLL.Common.Builders.Computers;
using MLL.Common.Builders.Weights;
using MLL.Common.Optimization;
using MLL.Common.Tools;
using Engines;

public abstract class RandomFillerNetFactory : NetFactory
{
    private readonly int _randomSeed;

    protected RandomFillerNetFactory(int randomSeed)
    {
        _randomSeed = randomSeed;
    }

    public override void PostCreation(ClassificationEngine net)
    {
        net.Weights.Layers.RandomFill(_randomSeed);
        base.PostCreation(net);
    }
}

public abstract class NetFactory : INetFactory
{
    public abstract LayerWeightsDefinition[] GetDefinitions();
    public abstract LayerComputerBuilderResult GetComputers(bool isForTrain);
    
    public virtual void PostCreation(ClassificationEngine net)
    {
    }

    public virtual ClassificationEngine Create(bool forTrain)
    {
        var weights = GetDefinitions().ToWeights();
        var computers = GetComputers(forTrain);

        var optimizator = new OptimizationManager(computers.Collectors);
        var buffer = NetLayersBuffers.CreateByWeights(weights);

        var net = new ClassificationEngine(computers.Computers, weights, optimizator, buffer);
        PostCreation(net);

        return net;
    }
}

public interface INetFactory
{
    ClassificationEngine Create(bool forTrain);
}
