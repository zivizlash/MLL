using MLL.Common.Tools;

namespace MLL.Common.Layer.Computers;

public interface IPredictComputer
{
    void Predict(LayerWeights layer, float[] input, float[] results, ProcessingRange range);
}
