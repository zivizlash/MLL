using MLL.Common.Layer;
using MLL.Common.Tools;
using System.Diagnostics.CodeAnalysis;

namespace MLL.Common.Net;

public struct NetReplicator
{
    public static Net Copy(Net source, Net computers, [NotNull] ref LayerWeights[]? buffer)
    {
        buffer ??= CreateEmptyCopy(source.Weights.Layers);
        CopyWeights(source.Weights.Layers, buffer);

        return new Net(computers.Computers.ToArray(), buffer, 
            computers.OptimizationManager, computers.Buffers);
    }

    private static LayerWeights[] CreateEmptyCopy(ReadOnlySpan<LayerWeights> src)
    {
        var copy = new LayerWeights[src.Length];

        for (int li = 0; li < src.Length; li++)
        {
            var srcLayer = src[li];
            var weightsCopy = copy[li] = new LayerWeights(new float[srcLayer.Weights.Length][]);

            for (int ni = 0; ni < srcLayer.Weights.Length; ni++)
            {
                weightsCopy.Weights[ni] = new float[srcLayer.Weights[ni].Length];
            }
        }

        return copy;
    }

    public static void CopyWeights(ReadOnlySpan<LayerWeights> src, LayerWeights[] dest)
    {
        Check.LengthEqual(src.Length, dest.Length, nameof(dest));

        for (int li = 0; li < src.Length; li++)
        {
            var srcLayer = src[li];
            var destLayer = dest[li];

            CopyInternal(srcLayer.Weights, destLayer.Weights);
        }
    }

    public static void CopyLayer(NetWeights src, NetWeights dest, int layerIndex)
    {
        var srcLayer = src.Layers[layerIndex];
        var destLayer = dest.Layers[layerIndex];

        CopyInternal(srcLayer.Weights, destLayer.Weights);
    }

    private static void CopyInternal(float[][] srcLayer, float[][] destLayer)
    {
        Check.LengthEqual(srcLayer.Length, destLayer.Length, nameof(destLayer));

        for (int ni = 0; ni < srcLayer.Length; ni++)
        {
            var srcWeights = srcLayer[ni];
            var destWeights = destLayer[ni];

            Check.LengthEqual(srcWeights.Length, destWeights.Length, nameof(destLayer));
            srcWeights.CopyTo(destWeights.AsSpan());
        }
    }
}
