using MLL.Layer.Computers.Sigmoid;
using MLL.Layer.Threading;

namespace MLL.Layer.Factories;

public class LayerComputerBuilder
    : LayerComputerBuilder.ILayerBuilderMaxThreads,
    LayerComputerBuilder.ILayerBuilderRequiredSamples,
    LayerComputerBuilder.ILayerBuilderOutlinersThreshold,
    LayerComputerBuilder.ILayerBuilderComputer
{
    #region Interfaces
    public interface ILayerBuilderMaxThreads
    {
        ILayerBuilderRequiredSamples WithMaxThreads(int threads);
        ILayerBuilderRequiredSamples WithMaxThreadsAsProccessorsCount();
    }

    public interface ILayerBuilderRequiredSamples
    {
        ILayerBuilderOutlinersThreshold WithRequiredSamples(int samples);
    }

    public interface ILayerBuilderOutlinersThreshold
    {
        ILayerBuilderComputer WithOutlinersThreshold(float percents);
    }

    public interface ILayerBuilderComputer
    {
        ILayerBuilderComputer UseLayer<TComputer>() where TComputer : ILayerDefinition;
        LayerComputerBuilderResult Build(bool forTrain = true);
    }
    #endregion

    private int _maxThreads;
    private float _outlinersThreshold;
    private int _requiredSamples;

    private readonly List<ThreadedProcessorStatCollector> _collectors = new();
    private readonly List<LayerComputers> _computers = new();
    private readonly List<ILayerComputerFactory> _factories = new();

    private readonly List<Type> _defTypes = new();

    public LayerComputerBuilder(params ILayerComputerFactory[] factories)
    {
        _factories.AddRange(factories);
        _factories.Add(new BasicLayerComputerFactory());
    }

    public ILayerBuilderRequiredSamples WithMaxThreads(int threads)
    {
        _maxThreads = threads;
        return this;
    }

    public ILayerBuilderRequiredSamples WithMaxThreadsAsProccessorsCount()
    {
        _maxThreads = Environment.ProcessorCount;
        return this;
    }

    ILayerBuilderComputer ILayerBuilderOutlinersThreshold.WithOutlinersThreshold(float percents)
    {
        _outlinersThreshold = percents;
        return this;
    }

    ILayerBuilderOutlinersThreshold ILayerBuilderRequiredSamples.WithRequiredSamples(int samples)
    {
        _requiredSamples = samples;
        return this;
    }

    private void Add<TComputer>()
    {
        if (!_factories.Any(f => f.IsCanResolve(typeof(TComputer))))
        { 
            throw new InvalidOperationException();
        }

        _defTypes.Add(typeof(TComputer));
    }

    ILayerBuilderComputer ILayerBuilderComputer.UseLayer<TComputer>()
    {
        Add<TComputer>();
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
                MaxThreads = _maxThreads,
                OutlinersThreshold = _outlinersThreshold,
                RequiredSamples = _requiredSamples,
                IsRequiredErrorBackpropagation = forTrain && typeIndex > 0,
                IsRequiredErrorCalculation = forTrain && typeIndex == _defTypes.Count - 1,
                IsRequiredCompensate = forTrain
            });

            _computers.Add(result.Computers);
            _collectors.AddRange(result.Collectors);
        }

        return new LayerComputerBuilderResult(_computers, _collectors);
    }
}
