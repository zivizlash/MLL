using MLL.Tools;

namespace MLL.Layer.Computers;

public interface ICalculateLayerComputer
{
    void CalculateErrors(float[] outputs, float[] expected, float[] errors);
}

public interface ICompensateLayerComputer
{
    void Compensate(LayerWeightsData layer, float[] input, float learningRate, float[] errors, float[] outputs);
}

public interface IPredictLayerComputer
{
    void Predict(LayerWeightsData layer, float[] input, float[] results);
}

public class LayerComputer
{
    public ICalculateLayerComputer Calculate { get; set; }
    public ICompensateLayerComputer Compensate { get; set; }
    public IPredictLayerComputer Predict { get; set; }

    public LayerComputer(IPredictLayerComputer predict, ICompensateLayerComputer compensate, 
        ICalculateLayerComputer calculate)
    {
        Predict = predict;
        Compensate = compensate;
        Calculate = calculate;
    }
}

public class SumLayerComputer : ILayerComputer
{
    public void CalculateErrors(float[] outputs, float[] expected, float[] errors)
    {
        Check.LengthEqual(outputs.Length, errors.Length, nameof(errors));
        Check.LengthEqual(outputs.Length, expected.Length, nameof(expected));

        for (int ri = 0; ri < outputs.Length; ri++)
        {
            errors[ri] = expected[ri] - outputs[ri];
        }
    }

    public void CompensateErrors(LayerWeightsData layer, float[] input, float learningRate, float[] errors, float[] outputs)
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

    public void Predict(LayerWeightsData layer, float[] input, float[] results)
    {
        var neurons = layer.Neurons;

        Check.LengthEqual(neurons.Length, results.Length, nameof(results));
        Check.LengthEqual(neurons[0].Length, input.Length, nameof(input));

        for (int ni = 0; ni < neurons.Length; ni++)
        {
            results[ni] = VectorCalculator.CalculateMultiplySum(neurons[ni], input);
        }
    }

    private static float GetGeneralError(float learningRate, float error)
    {
        return learningRate * error;
    }
}
