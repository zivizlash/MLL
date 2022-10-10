using MLL.Common.Factory;
using MLL.Common.Layer;
using MLL.Common.Optimization;

namespace MLL.Common.Builders.Computers;

public class LayerComputerBuilder : LayerComputerBuilder.ILayerBuilderComputer
{
    public interface ILayerBuilderComputer
    {
        ILayerBuilderComputer UseLayer<TComputer>() where TComputer : IFactoryLayerDefinition;
        LayerComputerBuilderResult Build(bool forTrain = true);
    }

    private readonly List<IOptimizator> _optimizators = new();
    private readonly List<LayerComputers> _computers = new();
    private readonly List<ILayerComputerFactory> _factories = new();

    private readonly List<Type> _defTypes = new();

    public LayerComputerBuilder(params ILayerComputerFactory[] factories)
    {
        _factories.AddRange(factories);
    }

    public ILayerBuilderComputer UseLayer<TComputer>() where TComputer : IFactoryLayerDefinition
    {
        if (!_factories.Any(f => f.IsCanResolve(typeof(TComputer))))
        {
            throw new InvalidOperationException();
        }

        _defTypes.Add(typeof(TComputer));
        return this;
    }

    LayerComputerBuilderResult ILayerBuilderComputer.Build(bool forTrain)
    {
        for (int typeIndex = 0; typeIndex < _defTypes.Count; typeIndex++)
        {
            var type = _defTypes[typeIndex];
            var factory = _factories.First(f => f.IsCanResolve(type));

            var result = factory.Resolve(type, new FactoryResolveParams
            {
                IsRequiredErrorBackpropagation = forTrain && typeIndex > 0,
                IsRequiredErrorCalculation = forTrain && typeIndex == _defTypes.Count - 1,
                IsRequiredCompensate = forTrain
            });

            _computers.Add(result.Computers);
            _optimizators.AddRange(result.Optimizators);
        }

        return new LayerComputerBuilderResult(_computers, _optimizators);
    }
}
