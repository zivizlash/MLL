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
        
        var computers = new LayerComputers(
            new SumCalculateComputer(), 
            CreatePredict(isSigmoid), 
            CreateCompensate(isSigmoid), 
            new ThreadedErrorBackpropagation());

        var optimizers = DecorateOptimizers(computers, arg).ToArray();

        return new FactoryResolveResult
        {
            Computers = computers,
            Optimizers = optimizers
        };
    }

    private IEnumerable<IOptimizator> DecorateOptimizers(LayerComputers computers, FactoryResolveParams param)
    {
        OptimizatorFactoryParams CreateParams(object threadedComputer) => 
            new((IThreadedComputer)threadedComputer, computers);

        // always using predict threading optimization
        {
            var arg = CreateParams(computers.Predict);
            var (predict, opt) = _factory.Create(computers.Predict, arg);
            computers.Predict = predict;
            yield return opt;
        }

        if (param.IsRequiredErrorCalculation)
        {
            var arg = CreateParams(computers.Calculate);
            var (calculate, opt) = _factory.Create(computers.Calculate, arg);
            computers.Calculate = calculate;
            yield return opt;
        }

        if (param.IsRequiredCompensate)
        {
            var arg = CreateParams(computers.Compensate);
            var (compensate, opt) = _factory.Create(computers.Compensate, arg);
            computers.Compensate = compensate;
            yield return opt;
        }

        if (param.IsRequiredErrorBackpropagation)
        {
            var arg = CreateParams(computers.ErrorBackpropagation);
            var (error, opt) = _factory.Create(computers.ErrorBackpropagation, arg);
            computers.ErrorBackpropagation = error;
            yield return opt;
        }
    }

    private static IPredictComputer CreatePredict(bool isSigmoid) =>
        isSigmoid ? new SigmoidPredictComputer() : new SumPredictComputer();
    private static ICompensateComputer CreateCompensate(bool isSigmoid) =>
        isSigmoid ? new SigmoidCompensateComputer() : new SumCompensateComputer();
}
