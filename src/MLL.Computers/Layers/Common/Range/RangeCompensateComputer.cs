using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Tools;

namespace MLL.Computers.Layers.Common.Range;

public class RangeCompensateComputer : ICompensateComputer
{
    private readonly ICompensateComputer _computer;
    private readonly ProcessingRange _range;

    public RangeCompensateComputer(ICompensateComputer computer, ProcessingRange range)
    {
        _computer = computer;
        _range = range;
    }

    public void Compensate(LayerWeights layer, float[] input, float learningRate, 
        float[] errors, float[] outputs, ProcessingRange _)
    {
        _computer.Compensate(layer, input, learningRate, errors, outputs, _range);
    }
}
