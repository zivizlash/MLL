namespace MLL.Common.Builders;

public struct LayerDefinition
{
    public static NeuronsDefinitionBuilder.IBuilderLearningRate Builder =>
        new NeuronsDefinitionBuilder();

    public int Layers { get; }
    public int NeuronsCount { get; }
    public int WeightsCount { get; }
    public bool UseActivationFunc { get; }

    public LayerDefinition(int layers, int neuronsCount, int weightsCount, bool useActivationFunc)
    {
        Layers = layers;
        NeuronsCount = neuronsCount;
        WeightsCount = weightsCount;
        UseActivationFunc = useActivationFunc;
    }

    public static LayerDefinition CreateSingle(int neuronsCount, int weightsCount, bool useActivationFunc = true) =>
        new(1, neuronsCount, weightsCount, useActivationFunc);
}
