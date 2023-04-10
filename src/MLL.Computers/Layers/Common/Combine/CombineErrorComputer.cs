using MLL.Common.Layer.Computers;
using MLL.Common.Tools;

namespace MLL.Computers.Layers.Common.Combine;

public class CombineErrorComputer : IErrorComputer
{
    private readonly IErrorComputer[] _computers;

    public CombineErrorComputer(IErrorComputer[] computers)
    {
        _computers = computers ?? throw new ArgumentNullException(nameof(computers));
    }

    public void CalculateErrors(float[] outputs, float[] expected,
        float[] errors, ProcessingRange range)
    {
        foreach (var computer in _computers)
        {
            computer.CalculateErrors(outputs, expected, errors, range);
        }
    }
}
