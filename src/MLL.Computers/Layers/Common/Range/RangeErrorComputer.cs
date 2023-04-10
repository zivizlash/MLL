using MLL.Common.Layer.Computers;
using MLL.Common.Tools;

namespace MLL.Computers.Layers.Common.Range;

public class RangeErrorComputer : IErrorComputer
{
    private readonly IErrorComputer _computer;
    private readonly ProcessingRange _range;

    public RangeErrorComputer(IErrorComputer computer, ProcessingRange range)
    {
        _computer = computer;
        _range = range;
    }

    public void CalculateErrors(float[] outputs, float[] expected, float[] errors, ProcessingRange _)
    {
        _computer.CalculateErrors(outputs, expected, errors, _range);
    }
}
