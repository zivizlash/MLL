using MLL.Common.Factory;
using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Optimization;
using MLL.Common.Threading;
using MLL.Computers.Factory.Defines;
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
        return type == typeof(SumLayerDefine) || type == typeof(SigmoidLayerDefine);
    }

    public FactoryResolveResult Resolve(Type type, FactoryResolveParams arg)
    {
        if (!IsCanResolve(type)) throw new InvalidOperationException();

        bool isSigmoid = typeof(SigmoidLayerDefine) == type;

        var calculateSource = CreateCalculate();
        var errorBackpropSource = CreateErrorbackprop();
        var predictSource = CreatePredict(isSigmoid);
        var compensateSource = CreateCompensate(isSigmoid);
        
        var computers = new LayerComputers(calculateSource, predictSource, compensateSource, errorBackpropSource);
        var optimizators = DecorateWithOptimizers(computers, arg).ToArray();

        return new FactoryResolveResult
        {
            Computers = computers,
            Optimizators = optimizators
        };
    }

    private List<IOptimizator> DecorateWithOptimizers(LayerComputers computers, FactoryResolveParams arg)
    {
        var optimizators = new List<IOptimizator>();

        computers.Calculate = AddIfNotNull(arg.IsRequiredErrorCalculation,
            _factory.Create(computers.Calculate, new((IThreadedComputer)computers.Calculate, computers)),
            computers.Calculate, optimizators);

        computers.Predict = AddIfNotNull(true,
            _factory.Create(computers.Predict, new((IThreadedComputer)computers.Predict, computers)),
            computers.Predict, optimizators);

        computers.Compensate = AddIfNotNull(arg.IsRequiredCompensate,
            _factory.Create(computers.Compensate, new((IThreadedComputer)computers.Compensate, computers)),
            computers.Compensate, optimizators);

        computers.ErrorBackpropagation = AddIfNotNull(arg.IsRequiredErrorBackpropagation,
            _factory.Create(computers.ErrorBackpropagation, new((IThreadedComputer)computers.ErrorBackpropagation, computers)),
            computers.ErrorBackpropagation, optimizators);

        return optimizators;
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

    private static ICalculateComputer CreateCalculate() =>
        new SumCalculateComputer { ThreadInfo = new(1) };

    private static IPredictComputer CreatePredict(bool isSigmoid) =>
        isSigmoid
        ? new SigmoidPredictComputer { ThreadInfo = new(1) }
        : new SumPredictComputer { ThreadInfo = new(1) };

    private static ICompensateComputer CreateCompensate(bool isSigmoid) =>
        isSigmoid
        ? new SigmoidCompensateComputer { ThreadInfo = new(1) }
        : new SumCompensateComputer { ThreadInfo = new(1) };
}
