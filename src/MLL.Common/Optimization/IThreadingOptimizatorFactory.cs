using MLL.Common.Layer.Backpropagation;
using MLL.Common.Layer.Computers;

namespace MLL.Common.Optimization;

public interface IThreadingOptimizatorFactory
{
    (ICompensateComputer, IOptimizator) Create(ICompensateComputer computer, OptimizatorFactoryParams param);
    (IPredictComputer, IOptimizator) Create(IPredictComputer computer, OptimizatorFactoryParams param);
    (IErrorComputer, IOptimizator) Create(IErrorComputer computer, OptimizatorFactoryParams param);
    (IErrorBackpropagation, IOptimizator) Create(IErrorBackpropagation errorBackprop, OptimizatorFactoryParams param);
}
