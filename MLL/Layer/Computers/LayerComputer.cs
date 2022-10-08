namespace MLL.Layer.Computers;

public class LayerComputer
{
    public ICalculateLayerComputer Calculate { get; set; }
    public ICompensateLayerComputer Compensate { get; set; }
    public IPredictLayerComputer Predict { get; set; }

    public LayerComputer(IPredictLayerComputer predict, ICompensateLayerComputer compensate, 
        ICalculateLayerComputer calculate)
    {
        Predict = predict;
        Compensate = compensate;
        Calculate = calculate;
    }
}
