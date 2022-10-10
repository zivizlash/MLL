using MLL.Common.Builders.Weights;

namespace MLL.Common.Builders;

public class LayerWeightsBuilder : LayerWeightsBuilder.IBuilder,
    LayerWeightsBuilder.IBuilderInputCount,
    LayerWeightsBuilder.IBuilderHiddenLayers,
    LayerWeightsBuilder.IBuilderOutputCount
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
        IBuilder WithOutputLayer(int neuronsCount);
    }

    public interface IBuilder
    {
        LayerDefinition[] Build();
    }
    #endregion

    private readonly List<int> _hiddenLayersCounts;
    private int _inputNeuronsCount;
    private int _inputWeights;
    private int? _outputNeuronsCount;

    public LayerWeightsBuilder()
    {
        _hiddenLayersCounts = new List<int>();
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

    public IBuilder WithOutputLayer(int neuronsCount)
    {
        _outputNeuronsCount = neuronsCount;
        return this;
    }

    public LayerDefinition[] Build()
    {
        if (_hiddenLayersCounts == null)
            throw new InvalidOperationException();

        var layersCount = _hiddenLayersCounts.Count + 1 + (_outputNeuronsCount ?? 0);

        var definition = new LayerDefinition[layersCount];

        int lastOutputCount = _inputNeuronsCount;

        const int layersDefinitionCount = 1;

        definition[0] = new LayerDefinition(layersDefinitionCount, _inputNeuronsCount, _inputWeights);

        for (int i = 0; i < _hiddenLayersCounts.Count; i++)
        {
            var neuronsCount = _hiddenLayersCounts[i];
            definition[i + 1] = new LayerDefinition(layersDefinitionCount, neuronsCount, lastOutputCount);
            lastOutputCount = neuronsCount;
        }

        if (_outputNeuronsCount.HasValue)
        {
            definition[^1] = new LayerDefinition(
                1, _outputNeuronsCount.Value, lastOutputCount);
        }

        return definition;
    }
}
