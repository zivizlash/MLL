namespace MLL.Common.Layer;

public readonly struct LayerWeights
{
    public readonly float[][] Weights;

    public LayerWeights(float[][] neurons)
    {
        Weights = neurons;
    }
}
