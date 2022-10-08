using MLL.Layer;
using MLL.Tools;
using System.Diagnostics.CodeAnalysis;

namespace MLL.Neurons;

public struct NetReplicator
{
    public static NetManager Copy(NetManager source, NetManager computers, [NotNull] ref LayerWeightsData[]? buffer)
    {
        buffer ??= CreateEmptyCopy(source.Weights);
        CopyWeights(source.Weights, buffer);
        return new NetManager(computers.Computers.ToArray(), buffer, computers.OptimizationManager);
    }

    private static LayerWeightsData[] CreateEmptyCopy(ReadOnlySpan<LayerWeightsData> src)
    {
        var copy = new LayerWeightsData[src.Length];

        for (int li = 0; li < src.Length; li++)
        {
            var srcLayer = src[li];
            var weightsCopy = copy[li] = new LayerWeightsData(new float[srcLayer.Neurons.Length][]);

            for (int ni = 0; ni < srcLayer.Neurons.Length; ni++)
            {
                weightsCopy.Neurons[ni] = new float[srcLayer.Neurons[ni].Length];
            }
        }

        return copy;
    }

    public static void CopyWeights(ReadOnlySpan<LayerWeightsData> src, LayerWeightsData[] dest)
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

    private static void CopyInternal(Net source, Net destination)
    {
        for (int li = 0; li < source.Layers.Length; li++)
        {
            var sourceLayer = source.Layers[li];
            var destinationLayer = destination.Layers[li];

            if (sourceLayer.Neurons.Length != destinationLayer.Neurons.Length)
                ThrowOutOfRange();

            for (int ni = 0; ni < sourceLayer.Neurons.Length; ni++)
            {
                var sourceNeuron = sourceLayer.Neurons[ni];
                var destinationNeuron = destinationLayer.Neurons[ni];

                if (sourceNeuron.Weights.Length != destinationNeuron.Weights.Length)
                    ThrowOutOfRange();

                sourceNeuron.Weights.CopyTo(destinationNeuron.Weights.AsSpan());
            }
        }
    }

    private static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException();
}
