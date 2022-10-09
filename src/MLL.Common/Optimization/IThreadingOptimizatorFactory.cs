using MLL.Common.Layer.Backpropagation;
using MLL.Common.Layer.Computers;

namespace MLL.Common.Optimization;

public interface IThreadingOptimizatorFactory
{
    (ICompensateLayerComputer, IOptimizator) Create(ICompensateLayerComputer computer, OptimizatorFactoryParams param);
    (IPredictLayerComputer, IOptimizator) Create(IPredictLayerComputer computer, OptimizatorFactoryParams param);
    (ICalculateLayerComputer, IOptimizator) Create(ICalculateLayerComputer computer, OptimizatorFactoryParams param);
    (IErrorBackpropagation, IOptimizator) Create(IErrorBackpropagation errorBackprop, OptimizatorFactoryParams param);
}
