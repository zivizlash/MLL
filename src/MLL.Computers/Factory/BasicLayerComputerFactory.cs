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
        var optimizers = DecorateOptimizers(computers, arg).ToArray();

        return new FactoryResolveResult
        {
            Computers = computers,
            Optimizers = optimizers
        };
    }

    private IEnumerable<IOptimizator> DecorateOptimizers(LayerComputers computers, FactoryResolveParams param)
    {
        OptimizatorFactoryParams CreateParams(object threadedComputer) => new((IThreadedComputer)threadedComputer, computers);

        // always using predict threading optimization
        {
            var (predict, opt) = _factory.Create(computers.Predict, CreateParams(computers.Predict));
            computers.Predict = predict;
            yield return opt;
        }

        if (param.IsRequiredErrorCalculation)
        {
            var (calculate, opt) = _factory.Create(computers.Calculate, CreateParams(computers.Calculate));
            computers.Calculate = calculate;
            yield return opt;
        }

        if (param.IsRequiredCompensate)
        {
            var (compensate, opt) = _factory.Create(computers.Compensate, CreateParams(computers.Compensate));
            computers.Compensate = compensate;
            yield return opt;
        }

        if (param.IsRequiredErrorBackpropagation)
        {
            var (error, opt) = _factory.Create(computers.ErrorBackpropagation, CreateParams(computers.ErrorBackpropagation));
            computers.ErrorBackpropagation = error;
            yield return opt;
        }
    }

    private static ThreadedErrorBackpropagation CreateErrorbackprop() => new();
    private static ICalculateComputer CreateCalculate() => new SumCalculateComputer();

    private static IPredictComputer CreatePredict(bool isSigmoid) =>
        isSigmoid ? new SigmoidPredictComputer() : new SumPredictComputer();
    private static ICompensateComputer CreateCompensate(bool isSigmoid) =>
        isSigmoid ? new SigmoidCompensateComputer() : new SumCompensateComputer();
}
