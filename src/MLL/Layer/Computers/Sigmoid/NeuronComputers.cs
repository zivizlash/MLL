using MLL.Layer.Backpropagation;

namespace MLL.Layer.Computers.Sigmoid;

public class NeuronComputers
{
    public ICalculateLayerComputer Calculate { get; set; }
    public IPredictLayerComputer Predict { get; set; }
    public ICompensateLayerComputer Compensate { get; set; }
    public IErrorBackpropagation ErrorBackpropagation { get; set; }
    
    public NeuronComputers(ICalculateLayerComputer calculate, IPredictLayerComputer predict, 
        ICompensateLayerComputer compensate, IErrorBackpropagation errorBackpropagation)
    {
        Calculate = calculate;
        Predict = predict;
        Compensate = compensate;
        ErrorBackpropagation = errorBackpropagation;
    }
}
