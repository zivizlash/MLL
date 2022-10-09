using MLL.Common.Layer;
using MLL.Common.Layer.Computers;
using MLL.Common.Tools;
using MLL.Tools;

namespace MLL.Layer.Computers.Sum;

public class SumPredictLayerComputer : IPredictLayerComputer
{
    public void Predict(LayerWeights layer, float[] input, float[] results)
    {
        var neurons = layer.Neurons;

        Check.LengthEqual(neurons.Length, results.Length, nameof(results));
        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));

        for (int ni = 0; ni < neurons.Length; ni++)
        {
            var sum = VectorCalculator.CalculateMultiplySum(neurons[ni], input);
            results[ni] = NumberTools.Sigmoid(sum);
        }
    }
}
