using MLL.Common.Layer;
using MLL.Common.Tools;
using System.Diagnostics.CodeAnalysis;

namespace MLL.Common.Net;

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

            Check.LengthEqual(srcLayer.Weights.Length, destLayer.Weights.Length, nameof(dest));

            for (int ni = 0; ni < srcLayer.Weights.Length; ni++)
            {
                var srcWeights = srcLayer.Weights[ni];
                var destWeights = destLayer.Weights[ni];

                Check.LengthEqual(srcWeights.Length, destWeights.Length, nameof(dest));
                srcWeights.CopyTo(destWeights.AsSpan());
            }
        }
    }
}
