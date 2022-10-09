using MLL.Common.Factory;
using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Threading;
using MLL.Layer.Backpropagation;
using MLL.Layer.Computers.Sigmoid;
using MLL.Layer.Computers.Sum;
using MLL.Layer.Threading;
using MLL.Layer.Threading.Adapters;
using MLL.Tools;

namespace MLL.Layer.Factories;

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

        var neuronComputers = new LayerComputers(calculateTimetracker, 
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
            Computers = neuronComputers,
            Optimizators = collectors
        };
    }

    private static ThreadedProcessorStatCollector CreateCollector(IThreadedComputer computer,
        ITimeTracker timeTracker, FactoryResolveParams arg, Action doneAction)
    {
        var threads = ListSelection.Range(arg.MaxThreads);
        var controller = new ThreadedProcessorController(computer, timeTracker);

        return new ThreadedProcessorStatCollector(controller, arg.RequiredSamples,
            arg.OutlinersThreshold, arg.MaxThreads, threads, doneAction);
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
}
