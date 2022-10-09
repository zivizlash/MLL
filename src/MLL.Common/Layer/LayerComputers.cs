using MLL.Common.Layer.Backpropagation;
using MLL.Common.Layer.Computers;

namespace MLL.Common.Layer;

public class LayerComputers
{
    public ICalculateComputer Calculate { get; set; }
    public IPredictComputer Predict { get; set; }
    public ICompensateComputer Compensate { get; set; }
    public IErrorBackpropagation ErrorBackpropagation { get; set; }
    
    public LayerComputers(ICalculateComputer calculate, IPredictComputer predict, 
        ICompensateComputer compensate, IErrorBackpropagation errorBackpropagation)
    {
        Calculate = calculate;
        Predict = predict;
        Compensate = compensate;
        ErrorBackpropagation = errorBackpropagation;
    }
}
