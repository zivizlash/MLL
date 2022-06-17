using System.Diagnostics.CodeAnalysis;

namespace MLL.Neurons;

public struct NetReplicator
{
    public static Net Copy(Net source)
    {
        var learningRate = source.Layers[0].Neurons[0].LearningRate;
        var copy = new Net(learningRate, source.GetDefinition());
        CopyInternal(source, copy);
        return copy;
    }

    public static void Copy(Net source, [NotNull] ref Net? destination)
    {
        if (destination == null)
        {
            destination = Copy(source);
            return;
        }

        CopyInternal(source, destination);
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
