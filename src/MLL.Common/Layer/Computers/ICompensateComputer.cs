using MLL.Common.Tools;

namespace MLL.Common.Layer.Computers;

public interface ICompensateComputer
{
    void Compensate(LayerWeights layer, float[] input, float learningRate, 
        float[] errors, float[] outputs, ProcessingRange range);
}
