namespace MLL.Common.Layer.Computers;

public interface ICompensateLayerComputer
{
    void Compensate(LayerWeights layer, float[] input, float learningRate, float[] errors, float[] outputs);
}
