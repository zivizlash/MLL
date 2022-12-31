using MLL.Common.Layer;
using System.Diagnostics.CodeAnalysis;

namespace MLL.Common.Engines;

public readonly struct NetWeights : IEquatable<NetWeights>
{
    public readonly LayerWeights[] Layers;

    public NetWeights(LayerWeights[] layers)
    {
        Layers = layers;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is NetWeights weights && Equals(weights);
    }

    public bool Equals(NetWeights other)
    {
        return Layers.SequenceEqual(other.Layers);
    }

    public override int GetHashCode()
    {
        return Layers.GetHashCode();
    }
}
