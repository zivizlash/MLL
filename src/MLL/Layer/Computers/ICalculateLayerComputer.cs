namespace MLL.Layer.Computers;

public interface ICalculateLayerComputer
{
    void CalculateErrors(float[] outputs, float[] expected, float[] errors);
}
