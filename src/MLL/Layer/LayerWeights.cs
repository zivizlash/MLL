namespace MLL.Layer;

public readonly struct LayerWeights
{
    public readonly float[][] Neurons;

    public LayerWeights(float[][] neurons)
    {
        Neurons = neurons;
    }
}
