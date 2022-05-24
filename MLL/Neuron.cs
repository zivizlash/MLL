namespace MLL;

public class Neuron
{
    private readonly double _bias;
    private readonly double _learningRate;
    private double[] _weights;
    private double[]? _results;

    public double LastError { get; private set; }

    public double[] Weights
    {
        get => _weights;
        set => _weights = value;
    }

    public Neuron(int weightCount, double bias, double learningRate)
    {
        _bias = bias;
        _learningRate = learningRate;
        _weights = new double[weightCount];
    }

    public Neuron FillRandomValues(Random random, double range = 4)
    {
        for (int i = 0; i < _weights.Length; i++)
            _weights[i] = random.NextDouble() * range - range;

        return this;
    }

    public double Predict(double[] input)
    {
        if (_weights.Length != input.Length)
            throw new InvalidOperationException();

        _results ??= new double[input.Length];
        
        for (int i = 0; i < input.Length; i++)
            _results[i] = _weights[i] * input[i];

        var sum = _results.Sum();
        LastError = _bias - sum;
        return sum >= _bias ? 1 : 0;
    }

    public double Train(double[] input, double expected)
    {
        if (_weights.Length != input.Length)
            throw new InvalidOperationException();

        var rawError = expected - Predict(input);

        var error = rawError;
        
        for (int i = 0; i < input.Length; i++)
            _weights[i] += error * input[i] * _learningRate;

        return error;
    }
}
