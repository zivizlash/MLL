namespace MLL.Common.Builders.Weights;

public struct LayerDefinition
{
    public static LayerWeightsBuilder.IBuilderInputCount Builder =>
        new LayerWeightsBuilder();

    public int Layers { get; }
    public int NeuronsCount { get; }
    public int WeightsCount { get; }

    public LayerDefinition(int layers, int neuronsCount, int weightsCount)
    {
        Layers = layers;
        NeuronsCount = neuronsCount;
        WeightsCount = weightsCount;
    }

    public static LayerDefinition CreateSingle(int neuronsCount, int weightsCount) => new(1, neuronsCount, weightsCount);
}
