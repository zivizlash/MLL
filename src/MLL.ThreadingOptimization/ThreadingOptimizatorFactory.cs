using MLL.Common.Layer;
using MLL.Common.Layer.Backpropagation;
using MLL.Common.Layer.Computers;
using MLL.Common.Layer.TimeTracking;
using MLL.Common.Optimization;
using MLL.Common.Threading;
using MLL.Common.Tools;

namespace MLL.ThreadingOptimization;

public class ThreadingOptimizatorFactory : IThreadingOptimizatorFactory
{
    private readonly ThreadingOptimizatorFactorySettings _settings;

    public ThreadingOptimizatorFactory(ThreadingOptimizatorFactorySettings settings)
    {
        _settings = settings;
    }

    public (ICompensateComputer, IOptimizator) Create(
        ICompensateComputer computer, OptimizatorFactoryParams param)
    {
        var timeTracker = new CompensateComputerTimeTracker(computer);
        return Create(timeTracker, param, с => с.Compensate = computer);
    }

    public (IPredictComputer, IOptimizator) Create(
        IPredictComputer computer, OptimizatorFactoryParams param)
    {
        var timeTracker = new PredictComputerTimeTracker(computer);
        return Create(timeTracker, param, с => с.Predict = computer);
    }

    public (IErrorComputer, IOptimizator) Create(
        IErrorComputer computer, OptimizatorFactoryParams param)
    {
        var timeTracker = new ErrorComputerTimeTracker(computer);
        return Create(timeTracker, param, с => с.Error = computer);
    }

    public (IErrorBackpropagation, IOptimizator) Create(
        IErrorBackpropagation errorBackprop, OptimizatorFactoryParams param)
    {
        var timeTracker = new ErrorBackpropogationTimeTracker(errorBackprop);
        return Create(timeTracker, param, с => с.ErrorBackpropagation = errorBackprop);
    }

    private (T, IOptimizator) Create<T>(T timeTrackerComputer, OptimizatorFactoryParams param,
        Action<LayerComputers> doneAction) where T : ITimeTracker
    {
        var controller = new ThreadedProcessorController(param.ThreadedComputer, timeTrackerComputer);
        var computers = param.LayerComputers;

        var optimizator = CreateOptimizator(controller, () => doneAction.Invoke(computers));
        return (timeTrackerComputer, optimizator);
    }

    private IOptimizator CreateOptimizator(ThreadedProcessorController controller, Action doneAction)
    {
        return new ThreadedProcessorStatCollector(controller, _settings.RequiredSamples,
            _settings.OutlinersThreshold, CreateThreadsSelection(), doneAction);
    }

    private ListSelection<int> CreateThreadsSelection()
    {
        return ListSelection.Range(_settings.MaxThreads);
    }
}
