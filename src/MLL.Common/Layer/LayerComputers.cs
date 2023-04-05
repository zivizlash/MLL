using MLL.Common.Layer.Backpropagation;
using MLL.Common.Layer.Computers;

namespace MLL.Common.Layer;

public class LayerComputers
{
    public IErrorComputer Error { get; set; }
    public IPredictComputer Predict { get; set; }
    public ICompensateComputer Compensate { get; set; }
    public IErrorBackpropagation ErrorBackpropagation { get; set; }
    
    public LayerComputers(IErrorComputer error, IPredictComputer predict, 
        ICompensateComputer compensate, IErrorBackpropagation errorBackpropagation)
    {
        Error = error;
        Predict = predict;
        Compensate = compensate;
        ErrorBackpropagation = errorBackpropagation;
    }
}
