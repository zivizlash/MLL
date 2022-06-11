namespace MLL.Builders;

public class NeuronsDefinitionBuilder : NeuronsDefinitionBuilder.IBuilder, 
    NeuronsDefinitionBuilder.IBuilderInputCount, 
    NeuronsDefinitionBuilder.IBuilderOutputCount, 
    NeuronsDefinitionBuilder.IBuilderHiddenLayers,
    NeuronsDefinitionBuilder.IBuilderLearningRate
{
    #region Interfaces
    
    public interface IBuilderInputCount
    {
        IBuilderHiddenLayers WithInput(int neuronsCount, int weights);
    }

    public interface IBuilderHiddenLayers
    {
        IBuilderOutputCount WithHiddenLayers(params int[] neuronsCounts);
    }

    public interface IBuilderOutputCount
    {
        IBuilder WithOutput(int neuronsCount, bool useActivationFunc);
    }

    public interface IBuilderLearningRate
    {
        IBuilderInputCount WithLearningRate(float learningRate);
    }

    public interface IBuilder
    {
        LayerDefinition[] Build();
    }

    #endregion

    private int _inputNeuronsCount;
    private int _inputWeights;
    private int[]? _hiddenLayersCount;
    private int _outputNeuronsCount;
    private float _learningRate;
    private bool _useActivationFunc;

    public IBuilderInputCount WithLearningRate(float learningRate)
    {
        _learningRate = learningRate;
        return this;
    }

    public IBuilderHiddenLayers WithInput(int neuronsCount, int weights)
    {
        _inputNeuronsCount = neuronsCount;
        _inputWeights = weights;
        return this;
    }

    public IBuilderOutputCount WithHiddenLayers(params int[] neuronsCounts)
    {
        _hiddenLayersCount = neuronsCounts;
        return this;
    }

    public IBuilder WithOutput(int neuronsCount, bool useActivationFunc)
    {
        _outputNeuronsCount = neuronsCount;
        _useActivationFunc = useActivationFunc;
        return this;
    }

    public LayerDefinition[] Build()
    {
        if (_hiddenLayersCount == null)
            throw new InvalidOperationException();

        var layersCount = 2 + _hiddenLayersCount.Length;
        var definition = new LayerDefinition[layersCount];

        int lastOutputCount = _inputNeuronsCount;

        const int layersDefinitionCount = 1;

        definition[0] = new LayerDefinition(layersDefinitionCount, _inputNeuronsCount, _inputWeights, true);

        for (int i = 0; i < _hiddenLayersCount.Length; i++)
        {
            var neuronsCount = _hiddenLayersCount[i];

            definition[i + 1] = new LayerDefinition(
                layersDefinitionCount, neuronsCount, lastOutputCount, true);

            lastOutputCount = neuronsCount;
        }

        definition[^1] = new LayerDefinition(1, _outputNeuronsCount, lastOutputCount, _useActivationFunc);
        return definition;
    }

}
