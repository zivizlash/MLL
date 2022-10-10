using MLL.Common.Builders.Weights;
using MLL.Common.Layer;

namespace MLL.CUI;

public static class DefinitionToWeights
{ 
    public static IEnumerable<LayerWeights> ToWeights(this IEnumerable<LayerDefinition> defs)
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
}
