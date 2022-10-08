namespace MLL.Layer;

public readonly struct LayerWeightsData
{
    public readonly float[][] Neurons;

    public LayerWeightsData(float[][] neurons)
    {
        Neurons = neurons;
    }
}
