using MLL.Layer.Computers.Sigmoid;
using MLL.Layer.Threading;

namespace MLL.Layer.Factories;

public class LayerComputerBuilder
    : LayerComputerBuilder.ILayerBuilderMaxThreads,
    LayerComputerBuilder.ILayerBuilderRequiredSamples,
    LayerComputerBuilder.ILayerBuilderOutlinersThreshold,
    LayerComputerBuilder.ILayerBuilderComputer
{
    public interface ILayerBuilderMaxThreads
    {
        ILayerBuilderRequiredSamples MaxThreads(int threads);
        ILayerBuilderRequiredSamples MaxThreadsAsProccessorsCount();
    }

    public interface ILayerBuilderRequiredSamples
    {
        ILayerBuilderOutlinersThreshold RequiredSamples(int samples);
    }

    public interface ILayerBuilderOutlinersThreshold
    {
        ILayerBuilderComputer OutlinersThreshold(float percents);
    }

    public interface ILayerBuilderComputer
    {
        ILayerBuilderComputer UseLayer<TComputer>();
        LayerComputerBuilderResult Build(bool forTrain = true);
    }

    //public interface ILayerBuilderInput
    //{
    //    ILayerBuilderHidden WithInput<TComputer>();
        
    //}

    //public interface ILayerBuilderHidden
    //{
    //    ILayerBuilderHidden WithHidden<TComputer>();
    //    ILayerBuilderOutput WithOutput<TComputer>();
    //}

    //public interface ILayerBuilderOutput
    //{
    //    LayerComputerBuilderResult Build(bool forTrain = true);
    //}

    private int _maxThreads;
    private float _outlinersThreshold;
    private int _requiredSamples;

    private readonly List<ThreadedProcessorStatCollector> _collectors = new();
    private readonly List<NeuronComputers> _computers = new();
    private readonly List<ILayerComputerFactory> _factories = new();

    private readonly List<Type> _defTypes = new();

    public LayerComputerBuilder(params ILayerComputerFactory[] factories)
    {
        _factories.AddRange(factories);
        _factories.Add(new BasicLayerComputerFactory());
    }

    public ILayerBuilderRequiredSamples MaxThreads(int threads)
    {
        _maxThreads = threads;
        return this;
    }

    public ILayerBuilderRequiredSamples MaxThreadsAsProccessorsCount()
    {
        _maxThreads = Environment.ProcessorCount;
        return this;
    }

    ILayerBuilderComputer ILayerBuilderOutlinersThreshold.OutlinersThreshold(float percents)
    {
        _outlinersThreshold = percents;
        return this;
    }

    ILayerBuilderOutlinersThreshold ILayerBuilderRequiredSamples.RequiredSamples(int samples)
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

    //ILayerBuilderHidden ILayerBuilderHidden.WithHidden<TComputer>()
    //{
    //    Add<TComputer>();
    //    return this;
    //}

    //ILayerBuilderOutput ILayerBuilderHidden.WithOutput<TComputer>()
    //{
    //    Add<TComputer>();
    //    return this;
    //}


        
    LayerComputerBuilderResult ILayerBuilderComputer.Build(bool forTrain)
    {
        for (int typeIndex = 0; typeIndex < _defTypes.Count; typeIndex++)
        {
            var type = _defTypes[typeIndex];
            var factory = _factories.FirstOrDefault(f => f.IsCanResolve(type));
            if (factory == null) throw new InvalidOperationException();

            var result = factory.Resolve(type, new FactoryResolveParams
            {
                MaxThreads = _maxThreads,
                OutlinersThreshold = _outlinersThreshold,
                RequiredSamples = _requiredSamples,
                IsRequiredErrorBackpropagation = forTrain && typeIndex > 0,
                IsRequiredErrorCalculation = forTrain && typeIndex == _defTypes.Count - 1,
                IsRequiredCompensate = forTrain
            });

            _computers.Add(result.NeuronComputers);
            _collectors.AddRange(result.Collectors);
        }

        return new LayerComputerBuilderResult(_computers, _collectors);
    }
}
