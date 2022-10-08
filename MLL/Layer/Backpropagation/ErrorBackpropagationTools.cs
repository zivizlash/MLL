using MLL.Tools;

namespace MLL.Layer.Backpropagation;

public static class ErrorBackpropagationTools
{
    public static void CalculateNeuronError(float[] weights, float error, float[] errors)
    {
        var weightsSum = VectorCalculator.CalculateAbsSum(weights);

        for (int weightIndex = 0; weightIndex < weights.Length; weightIndex++)
        {
            var weightValue = weights[weightIndex];
            var errorPart = Math.Abs(weightValue) / weightsSum * error;
            errors[weightIndex] += errorPart;
        }
    }
}
