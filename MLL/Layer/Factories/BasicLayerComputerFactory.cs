using MLL.Layer.Backpropagation;
using MLL.Layer.Computers;
using MLL.Layer.Computers.Sigmoid;
using MLL.Layer.Computers.Sum;
using MLL.Layer.Threading;
using MLL.Layer.Threading.Adapters;
using MLL.Tools;

namespace MLL.Layer.Factories;

public struct FactoryResolveResult
{
    public ThreadedProcessorStatCollector[] Collectors;
    public NeuronComputers NeuronComputers;
}

public struct FactoryResolveParams
{
    public int MaxThreads;
    public float OutlinersThreshold;
    public int RequiredSamples;
    public bool IsRequiredErrorBackpropagation;
    public bool IsRequiredErrorCalculation;
    public bool IsRequiredCompensate;
}

public interface ILayerComputerFactory
{
    bool IsCanResolve(Type type);
    FactoryResolveResult Resolve(Type type, FactoryResolveParams arg);
}

public class SumLayerDef
{
}

public class SigmoidLayerDef
{
}

public class BasicLayerComputerFactory : ILayerComputerFactory
{
    public bool IsCanResolve(Type type)
    {
        return type == typeof(SumLayerDef) || type == typeof(SigmoidLayerDef);
    }

    public FactoryResolveResult Resolve(Type type, FactoryResolveParams arg)
    {
        if (!IsCanResolve(type)) throw new InvalidOperationException();

        bool isSigmoid = typeof(SigmoidLayerDef) == type;

        var calculate = CreateCalculate();
        var predict = CreatePredict(isSigmoid);
        var compensate = CreateCompensate(isSigmoid);

        var errorBackprop = new ThreadedErrorBackpropagation();
        var backpropTimetracker = new ErrorBackpropogationTimeTrackerDecorator(errorBackprop);

        var compensateTimetracker = new CompensateLayerProcessorTimeTrackerDecorator(compensate);
        var predictTimetracker = new PredictLayerProcessorTimeTrackerDecorator(predict);
        var calculateTimetracker = new CalculateLayerProcessorTimeTrackerDecorator(calculate);

        var neuronComputers = new NeuronComputers(calculateTimetracker, 
            predictTimetracker, compensateTimetracker, backpropTimetracker);

        var predictCollector = CreateCollector((IThreadedComputer)predict, predictTimetracker, arg,
            AddMessage((IThreadedComputer)predict, () => neuronComputers.Predict = predict));

        var compensateCollector = arg.IsRequiredCompensate
            ? CreateCollector((IThreadedComputer)compensate, compensateTimetracker, arg,
                AddMessage((IThreadedComputer)compensate, () => neuronComputers.Compensate = compensate))
            : null;

        var backpropCollector = arg.IsRequiredErrorBackpropagation 
            ? CreateCollector(errorBackprop, backpropTimetracker, arg,
                AddMessage(errorBackprop, () => neuronComputers.ErrorBackpropagation = errorBackprop))
            : null;

        var calculateCollector = arg.IsRequiredErrorCalculation
            ? CreateCollector((IThreadedComputer)calculate, calculateTimetracker, arg,
                AddMessage((IThreadedComputer)calculate, () => neuronComputers.Calculate = calculate))
            : null;

        var collectors = new ThreadedProcessorStatCollector[]
        {
            predictCollector, compensateCollector!,
            calculateCollector!, backpropCollector!
        }.Where(c => c != null).ToArray();

        return new FactoryResolveResult
        {
            NeuronComputers = neuronComputers,
            Collectors = collectors
        };
    }

    private static ICalculateLayerComputer CreateCalculate() =>
        new ThreadedSumCalculateLayerComputer { ThreadInfo = new(1) };

    private static IPredictLayerComputer CreatePredict(bool isSigmoid) =>
        isSigmoid
        ? new ThreadedSigmoidPredictLayerComputer { ThreadInfo = new(1) }
        : new ThreadedSumPredictLayerComputer { ThreadInfo = new(1) };

    private static ICompensateLayerComputer CreateCompensate(bool isSigmoid) =>
        isSigmoid
        ? new ThreadedSigmoidCompensateLayerComputer { ThreadInfo = new(1) }
        : new ThreadedSumCompensateLayerComputer { ThreadInfo = new(1) };

    private static Action AddMessage(IThreadedComputer threadedComputer, Action action) =>
        action + (() => Console.WriteLine($"Optimized with: {threadedComputer.ThreadInfo.Threads} threads"));

    private static ThreadedProcessorStatCollector CreateCollector(IThreadedComputer computer, 
        ITimeTracker timeTracker, FactoryResolveParams arg, Action doneAction)
    {
        var threads = ListSelection.Range(arg.MaxThreads);
        var controller = new ThreadedProcessorController(computer, timeTracker);

        return new ThreadedProcessorStatCollector(controller, arg.RequiredSamples,
            arg.OutlinersThreshold, arg.MaxThreads, threads, doneAction);
    }
}

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
