using MLL.Common.Engines;
using MLL.Common.Layer;
using MLL.Common.Tools;
using System.Diagnostics.CodeAnalysis;

namespace MLL.Race.Web.Server;

public class WeightsRasterizer
{
    public WeightsOffset FindOffset(NetWeights from, NetWeights to, [NotNull] ref LayerWeights[]? offsetBuffer)
    {
        Check.LengthEqual(from.Layers.Length, to.Layers.Length, nameof(to));
        offsetBuffer ??= NetReplicator.CopyWeights(from.Layers);
        Check.LengthEqual(from.Layers.Length, offsetBuffer.Length, nameof(offsetBuffer));

        var fromLayers = from.Layers;
        var toLayers = to.Layers;

        for (int i = 0; i < from.Layers.Length; i++)
        {
            var fromLayer = fromLayers[i].Weights;
            var toLayer = toLayers[i].Weights;
            var offsetsLayer = offsetBuffer[i].Weights;

            for (int neuronIndex = 0; neuronIndex < fromLayer.Length; neuronIndex++)
            {
                var fromNeuron = fromLayer[neuronIndex];
                var toNeuron = toLayer[neuronIndex];
                var offset = offsetsLayer[neuronIndex]; 

                for (int weightIndex = 0; weightIndex < fromNeuron.Length; weightIndex++)
                {
                    offset[weightIndex] = toNeuron[weightIndex] - fromNeuron[weightIndex];
                }
            }
        }

        return new WeightsOffset(new(offsetBuffer));
    }
}
