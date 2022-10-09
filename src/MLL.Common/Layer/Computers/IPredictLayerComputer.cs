namespace MLL.Common.Layer.Computers;

public interface IPredictLayerComputer
{
    void Predict(LayerWeights layer, float[] input, float[] results);
}
