using MLL.Layer.Threading;

namespace MLL.Layer.Computers;

public interface ILayerComputer
{
    void CalculateErrors(float[] outputs, float[] expected, float[] errors);
    void Predict(LayerWeightsData layer, float[] input, float[] results);
    void CompensateErrors(LayerWeightsData layer, float[] input, float learningRate, float[] errors, float[] outputs);
}

public interface IThreadLayerComputer
{
    LayerThreadInfo CalculateThreadInfo { get; set; }
    LayerThreadInfo PredictThreadInfo { get; set; }
    LayerThreadInfo CompensateThreadInfo { get; set; }
}
