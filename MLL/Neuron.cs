using System.Runtime.InteropServices;

namespace MLL;

public class Neuron
{
    private readonly double _bias;
    private readonly double _learningRate;
    private readonly double[] _weights;

    public double LastError { get; private set; }
    
    public Neuron(int weightCount, double bias, double learningRate)
    {
        _bias = bias;
        _learningRate = learningRate;
        _weights = new double[weightCount];
    }

    public Neuron FillRandomValues(Random random)
    {
        for (int i = 0; i < _weights.Length; i++)
            _weights[i] = random.NextDouble() * 4 - 4;

        return this;
    }

    public double Predict(double[] input)
    {
        if (_weights.Length != input.Length)
            throw new InvalidOperationException();

        var results = new double[input.Length];

        for (int i = 0; i < input.Length; i++)
            results[i] = _weights[i] * input[i];

        var sum = results.Sum();
        LastError = _bias - sum;
        return sum >= _bias ? 1 : 0;
    }

    public double Train(double[] input, double expected)
    {
        if (_weights.Length != input.Length)
            throw new InvalidOperationException();

        var rawError = expected - Predict(input);

        var error = Math.Abs(rawError) * rawError;
        
        for (int i = 0; i < input.Length; i++)
            _weights[i] += error * input[i] * _learningRate;

        return error;
    }
}
