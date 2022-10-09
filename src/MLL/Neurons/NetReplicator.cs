using MLL.Layer;
using MLL.Tools;
using System.Diagnostics.CodeAnalysis;

namespace MLL.Neurons;

public struct NetReplicator
{
    public static NetManager Copy(NetManager source, NetManager computers, [NotNull] ref LayerWeights[]? buffer)
    {
        buffer ??= CreateEmptyCopy(source.Weights);
        CopyWeights(source.Weights, buffer);
        return new NetManager(computers.Computers.ToArray(), buffer, computers.OptimizationManager);
    }

    private static LayerWeights[] CreateEmptyCopy(ReadOnlySpan<LayerWeights> src)
    {
        var copy = new LayerWeights[src.Length];

        for (int li = 0; li < src.Length; li++)
        {
            var srcLayer = src[li];
            var weightsCopy = copy[li] = new LayerWeights(new float[srcLayer.Neurons.Length][]);

            for (int ni = 0; ni < srcLayer.Neurons.Length; ni++)
            {
                weightsCopy.Neurons[ni] = new float[srcLayer.Neurons[ni].Length];
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

            Check.LengthEqual(srcLayer.Neurons.Length, destLayer.Neurons.Length, nameof(dest));

            for (int ni = 0; ni < srcLayer.Neurons.Length; ni++)
            {
                var srcWeights = srcLayer.Neurons[ni];
                var destWeights = destLayer.Neurons[ni];

                Check.LengthEqual(srcWeights.Length, destWeights.Length, nameof(dest));
                srcWeights.CopyTo(destWeights.AsSpan());
            }
        }
    }
}
