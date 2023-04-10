using MLL.Common.Tools;

namespace MLL.Common.Layer.Computers;

public interface IErrorComputer
{
    void CalculateErrors(float[] outputs, float[] expected, float[] errors, ProcessingRange range);
}
