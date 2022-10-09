namespace MLL.Common.Layer.Backpropagation;

public readonly struct BackpropContext
{
    public readonly float[][] Neurons;
    public readonly float[] Errors;

    public BackpropContext(float[][] neurons, float[] errors)
    {
        Neurons = neurons;
        Errors = errors;
    }
}
