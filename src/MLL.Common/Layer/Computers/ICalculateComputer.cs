namespace MLL.Common.Layer.Computers;

public interface ICalculateComputer
{
    void CalculateErrors(float[] outputs, float[] expected, float[] errors);
}
