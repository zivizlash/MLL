using MLL.Common.Factory;
using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Optimization;
using MLL.Common.Threading;
using MLL.Computers.Layers.Backpropagation;
using MLL.Computers.Layers.Sigmoid;
using MLL.Computers.Layers.Sum;

namespace MLL.Computers.Factory;

public class BasicLayerComputerFactory : ILayerComputerFactory
{
    private readonly IThreadingOptimizatorFactory _factory;

    public BasicLayerComputerFactory(IThreadingOptimizatorFactory optimizationFactory)
    {
        _factory = optimizationFactory;
    }

    public bool IsCanResolve(Type type)
    {
        return type == typeof(SumLayerDef) || type == typeof(SigmoidLayerDef);
    }

    public FactoryResolveResult Resolve(Type type, FactoryResolveParams arg)
    {
        if (!IsCanResolve(type)) throw new InvalidOperationException();

        bool isSigmoid = typeof(SigmoidLayerDef) == type;

        var calculateSource = CreateCalculate();
        var errorBackpropSource = CreateErrorbackprop();
        var predictSource = CreatePredict(isSigmoid);
        var compensateSource = CreateCompensate(isSigmoid);
        
        var computers = new LayerComputers(calculateSource, predictSource, compensateSource, errorBackpropSource);
        var optimizators = new List<IOptimizator>();

        var calculate = AddIfNotNull(arg.IsRequiredErrorCalculation, 
            _factory.Create(calculateSource, new((IThreadedComputer)calculateSource, computers)),
            calculateSource, optimizators);

        var predict = AddIfNotNull(true,
            _factory.Create(predictSource, new((IThreadedComputer)predictSource, computers)),
            predictSource, optimizators);

        var compensate = AddIfNotNull(arg.IsRequiredCompensate,
            _factory.Create(compensateSource, new((IThreadedComputer)compensateSource, computers)),
            compensateSource, optimizators);

        var errorBackprop = AddIfNotNull(arg.IsRequiredErrorBackpropagation,
            _factory.Create(errorBackpropSource, new(errorBackpropSource, computers)),
            errorBackpropSource, optimizators);

        computers.Calculate = calculate;
        computers.Predict = predict;
        computers.Compensate = compensate;
        computers.ErrorBackpropagation = errorBackprop;

        return new FactoryResolveResult
        {
            Computers = computers,
            Optimizators = optimizators.ToArray()
        };
    }

    private T AddIfNotNull<T>(bool required, (T, IOptimizator) results, T source, List<IOptimizator> optimizators)
    {
        if (required)
        {
            optimizators.Add(results.Item2);
            return results.Item1;
        }

        return source;
    }

    private static ThreadedErrorBackpropagation CreateErrorbackprop() =>
        new ThreadedErrorBackpropagation() { ThreadInfo = new(1) };

    private static ICalculateLayerComputer CreateCalculate() =>
        new SumCalculateLayerComputer { ThreadInfo = new(1) };

    private static IPredictLayerComputer CreatePredict(bool isSigmoid) =>
        isSigmoid
        ? new SigmoidPredictLayerComputer { ThreadInfo = new(1) }
        : new SumPredictLayerComputer { ThreadInfo = new(1) };

    private static ICompensateLayerComputer CreateCompensate(bool isSigmoid) =>
        isSigmoid
        ? new SigmoidCompensateLayerComputer { ThreadInfo = new(1) }
        : new SumCompensateLayerComputer { ThreadInfo = new(1) };
}
