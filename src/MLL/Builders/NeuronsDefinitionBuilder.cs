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
        IBuilderHiddenLayers WithInputLayer(int neuronsCount, int weights);
    }

    public interface IBuilderHiddenLayers
    {
        IBuilderOutputCount WithHiddenLayers(params int[] neuronsCounts);
        IBuilderHiddenLayers WithHiddenLayer(int neurons);
    }

    public interface IBuilderOutputCount
    {
        IBuilder WithoutOutputLayer();
        IBuilder WithOutputLayer(int neuronsCount, bool useActivationFunc);
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
    private readonly List<int> _hiddenLayersCounts;
    private int? _outputNeuronsCount;
    private float _learningRate;
    private bool _useActivationFunc;

    public NeuronsDefinitionBuilder()
    {
        _hiddenLayersCounts = new List<int>();
    }

    public IBuilderInputCount WithLearningRate(float learningRate)
    {
        _learningRate = learningRate;
        return this;
    }

    public IBuilderHiddenLayers WithInputLayer(int neuronsCount, int weights)
    {
        _inputNeuronsCount = neuronsCount;
        _inputWeights = weights;
        return this;
    }

    public IBuilderHiddenLayers WithHiddenLayer(int neuronsCount)
    {
        _hiddenLayersCounts.Add(neuronsCount);
        return this;
    }

    public IBuilderOutputCount WithHiddenLayers(params int[] neuronsCounts)
    {
        _hiddenLayersCounts.AddRange(neuronsCounts);
        return this;
    }

    public IBuilder WithoutOutputLayer()
    {
        _outputNeuronsCount = null;
        return this;
    }

    public IBuilder WithOutputLayer(int neuronsCount, bool useActivationFunc)
    {
        _outputNeuronsCount = neuronsCount;
        _useActivationFunc = useActivationFunc;
        return this;
    }

    public LayerDefinition[] Build()
    {
        if (_hiddenLayersCounts == null)
            throw new InvalidOperationException();

        var layersCount = _hiddenLayersCounts.Count 
            + 1 + (_outputNeuronsCount.HasValue ? 1 : 0);

        var definition = new LayerDefinition[layersCount];

        int lastOutputCount = _inputNeuronsCount;

        const int layersDefinitionCount = 1;

        definition[0] = new LayerDefinition(
            layersDefinitionCount, _inputNeuronsCount, _inputWeights, true);
        
        for (int i = 0; i < _hiddenLayersCounts.Count; i++)
        {
            var neuronsCount = _hiddenLayersCounts[i];

            definition[i + 1] = new LayerDefinition(
                layersDefinitionCount, neuronsCount, lastOutputCount, true);

            lastOutputCount = neuronsCount;
        }
        
        if (_outputNeuronsCount.HasValue)
        {
            definition[^1] = new LayerDefinition(
                1, _outputNeuronsCount.Value, lastOutputCount, _useActivationFunc);
        }

        return definition;
    }
}
