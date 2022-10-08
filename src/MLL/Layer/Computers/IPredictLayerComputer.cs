namespace MLL.Layer.Computers;

public interface IPredictLayerComputer
{
    void Predict(LayerWeightsData layer, float[] input, float[] results);
}
