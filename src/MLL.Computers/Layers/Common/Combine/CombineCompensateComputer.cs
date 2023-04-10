using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Tools;

namespace MLL.Computers.Layers.Common.Combine;

public class CombineCompensateComputer : ICompensateComputer
{
    private readonly ICompensateComputer[] _computers;

    public CombineCompensateComputer(ICompensateComputer[] computers)
    {
        _computers = computers ?? throw new ArgumentNullException(nameof(computers));
    }

    public void Compensate(LayerWeights layer, float[] input, float learningRate,
        float[] errors, float[] outputs, ProcessingRange range)
    {
        foreach (var computer in _computers)
        {
            computer.Compensate(layer, input, learningRate, errors, outputs, range);
        }
    }
}
