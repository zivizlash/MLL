namespace MLL.Common.Builders.Weights;

public struct LayerWeightsDefinition
{
    public static LayerWeightsBuilder.IBuilderInputCount Builder =>
        new LayerWeightsBuilder();

    public int Layers { get; }
    public int NeuronsCount { get; }
    public int WeightsCount { get; }

    public LayerWeightsDefinition(int layers, int neuronsCount, int weightsCount)
    {
        Layers = layers;
        NeuronsCount = neuronsCount;
        WeightsCount = weightsCount;
    }

    public static LayerWeightsDefinition CreateSingle(int neuronsCount, int weightsCount) => new(1, neuronsCount, weightsCount);
}
