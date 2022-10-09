using MLL.Tools;

namespace MLL.Layer.Computers.Sum;

public class SumCompensateLayerComputer : ICompensateLayerComputer
{
    public void Compensate(LayerWeights layer, float[] input, float learningRate, float[] errors, float[] outputs)
    {
        var neurons = layer.Neurons;

        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));
        Check.LengthEqual(neurons.Length, errors.Length, nameof(errors));
        Check.LengthEqual(neurons.Length, outputs.Length, nameof(outputs));

        for (int ni = 0; ni < neurons.Length; ni++)
        {
            var weights = neurons[ni];
            var generalError = GetGeneralError(learningRate, errors[ni]);

            for (int wi = 0; wi < weights.Length; wi++)
            {
                weights[wi] += generalError * input[wi];
            }
        }
    }

    private static float GetGeneralError(float learningRate, float error)
    {
        return learningRate * error;
    }
}
