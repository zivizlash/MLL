using System.Runtime.CompilerServices;

namespace MLL;

public class SigmoidNeuron : Neuron
{
    private readonly bool _useActivationFunc;

    public SigmoidNeuron(int weightCount, double learningRate, bool useActivationFunc)
        : base(weightCount, learningRate)
    {
        _useActivationFunc = useActivationFunc;
    }
    
    public override double Predict(double[] input)
    {
        var sum = CalculateWeightMultiplySum(input);

        return _useActivationFunc
            ? NumberTools.Sigmoid(sum)
            : sum;
    }

    public void CompensateError(double[] input, NeuronError neuronError)
    {
        var deltaWeight = GetDeltaWeight(neuronError.Output, neuronError.Error);

        for (int i = 0; i < Weights.Length; i++)
        {
            Weights[i] -= deltaWeight * input[i];
        }
    }
    
    public NeuronError CalculateError(double[] input, double expected)
    {
        var output = Predict(input);
        var error = -(expected - output);
        return new NeuronError(error, output);
    }

    public override double Train(double[] input, double expected)
    {
        var output = Predict(input);
        var error = -(expected - output);

        var deltaWeight = GetDeltaWeight(output, error);

        for (int i = 0; i < Weights.Length; i++)
        {
            var delta = deltaWeight * input[i];
            Weights[i] -= delta;
        }

        return error;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetDeltaWeight(double output, double error)
    {
        if (_useActivationFunc)
        {
            double sigmoid = output * (1.0 - output);
            return LearningRate * error * sigmoid;
        }

        return LearningRate * error;
    }
}
