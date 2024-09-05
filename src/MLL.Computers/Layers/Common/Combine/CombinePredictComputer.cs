using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Tools;

namespace MLL.Computers.Layers.Common.Combine;

public class CombinePredictComputer : IPredictComputer
{
    private readonly IPredictComputer[] _computers;

    public CombinePredictComputer(IPredictComputer[] computers)
    {
        _computers = computers ?? throw new ArgumentNullException(nameof(computers));
    }

    public void Predict(LayerWeights layer, float[] input, float[] results, ProcessingRange range)
    {
        foreach (var computer in _computers)
        {
            computer.Predict(layer, input, results, range);
        }
    }
}
