using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Tools;

namespace MLL.Computers.Layers.Common.ReadOnly;

public class ReadOnlyCompensateComputer : ICompensateComputer
{
    public void Compensate(LayerWeights layer, float[] input, float learningRate, 
        float[] errors, float[] outputs, ProcessingRange range)
    {
        // do nothing.
    }
}
