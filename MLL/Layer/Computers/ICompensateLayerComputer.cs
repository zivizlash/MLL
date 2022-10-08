namespace MLL.Layer.Computers;

public interface ICompensateLayerComputer
{
    void Compensate(LayerWeightsData layer, float[] input, float learningRate, float[] errors, float[] outputs);
}
