using MLL.Common.Builders.Weights;
using MLL.Common.Layer;

namespace MLL.CUI;

public static class WeightsExtensions
{ 
    public static IEnumerable<LayerWeights> ToWeights(this IEnumerable<LayerWeightsDefinition> defs)
    {
        foreach (var def in defs)
        {
            var neurons = new float[def.NeuronsCount][];

            for (int i = 0; i < neurons.Length; i++)
            {
                neurons[i] = new float[def.WeightsCount];
            }

            yield return new LayerWeights(neurons);
        }
    }

    public static LayerWeights[] RandomFill(this LayerWeights[] weights, int seed)
    {
        var rnd = new Random(seed);

        foreach (var neuron in weights.SelectMany(w => w.Weights))
        {
            for (int i = 0; i < neuron.Length; i++)
            {
                neuron[i] = rnd.NextSingle() * 2 - 1;
            }
        }

        return weights;
    }
}
