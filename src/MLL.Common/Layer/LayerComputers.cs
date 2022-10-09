using MLL.Common.Layer.Backpropagation;
using MLL.Common.Layer.Computers;

namespace MLL.Common.Layer;

public class LayerComputers
{
    public ICalculateLayerComputer Calculate { get; set; }
    public IPredictLayerComputer Predict { get; set; }
    public ICompensateLayerComputer Compensate { get; set; }
    public IErrorBackpropagation ErrorBackpropagation { get; set; }
    
    public LayerComputers(ICalculateLayerComputer calculate, IPredictLayerComputer predict, 
        ICompensateLayerComputer compensate, IErrorBackpropagation errorBackpropagation)
    {
        Calculate = calculate;
        Predict = predict;
        Compensate = compensate;
        ErrorBackpropagation = errorBackpropagation;
    }
}
