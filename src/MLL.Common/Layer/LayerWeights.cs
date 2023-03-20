using System.Diagnostics.CodeAnalysis;

namespace MLL.Common.Layer;

public readonly struct LayerWeights : IEquatable<LayerWeights>
{
    public readonly float[][] Weights;
    
    public LayerWeights(float[][] neurons)
    {
        Weights = neurons;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is LayerWeights weights && Equals(weights);
    }

    public bool Equals(LayerWeights other)
    {
        if (Weights.Length != other.Weights.Length) return false;

        for (int i = 0; i < Weights.Length; i++)
        {
            if (!Weights[i].SequenceEqual(other.Weights[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        return Weights.GetHashCode();
    }
}
