using MLL.Common.Builders.Weights;

namespace MLL.Common.Builders;

public class LayerWeightsBuilder : LayerWeightsBuilder.IBuilder,
    LayerWeightsBuilder.IBuilderInputCount,
    LayerWeightsBuilder.IBuilderHiddenLayers
{
    #region Interfaces
    public interface IBuilderInputCount
    {
        IBuilderHiddenLayers WithInputLayer(int neuronsCount, int weights);
    }

    public interface IBuilderHiddenLayers
    {
        IBuilderHiddenLayers WithLayer(int neurons);
        LayerWeightsDefinition[] Build();
    }

    public interface IBuilderOutputCount
    {
        IBuilder WithoutOutputLayer();
        IBuilder WithOutputLayer(int neuronsCount);
    }

    public interface IBuilder
    {
        LayerWeightsDefinition[] Build();
    }
    #endregion

    private readonly List<int> _layersCounts;
    private int _inputNeuronsCount;
    private int _inputWeights;

    public LayerWeightsBuilder()
    {
        _layersCounts = new List<int>();
    }

    public IBuilderHiddenLayers WithInputLayer(int neuronsCount, int weights)
    {
        _inputNeuronsCount = neuronsCount;
        _inputWeights = weights;
        return this;
    }

    public IBuilderHiddenLayers WithLayer(int neuronsCount)
    {
        _layersCounts.Add(neuronsCount);
        return this;
    }

    public LayerWeightsDefinition[] Build()
    {
        var layersCount = _layersCounts.Count + 1;
        var definition = new LayerWeightsDefinition[layersCount];

        definition[0] = new LayerWeightsDefinition(1, _inputNeuronsCount, _inputWeights);

        int lastOutputCount = _inputNeuronsCount;

        for (int i = 0; i < _layersCounts.Count; i++)
        {
            var neuronsCount = _layersCounts[i];
            definition[i + 1] = new LayerWeightsDefinition(1, neuronsCount, lastOutputCount);
            lastOutputCount = neuronsCount;
        }

        return definition;
    }
}
