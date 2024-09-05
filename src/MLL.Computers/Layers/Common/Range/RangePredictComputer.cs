using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Tools;

namespace MLL.Computers.Layers.Common.Range;

public class RangePredictComputer : IPredictComputer
{
    private readonly IPredictComputer _computer;
    private readonly ProcessingRange _range;

    public RangePredictComputer(IPredictComputer computer, ProcessingRange range)
    {
        _computer = computer;
        _range = range;
    }

    public void Predict(LayerWeights layer, float[] input, float[] results, ProcessingRange _)
    {
        _computer.Predict(layer, input, results, _range);
    }
}
