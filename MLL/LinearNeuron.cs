namespace MLL;

public interface INeuron
{
    public double LastError { get; }
    public double[] Weights { get; set; }

    INeuron FillRandomValues(Random random, double range = 4);
    double Predict(double[] input);
    double Train(double[] input, double expected);
}

public class LinearNeuron : INeuron
{
    private readonly double _bias;
    private readonly double _learningRate;
    private double[] _weights;
    
    public double LastError { get; private set; }

    public double[] Weights
    {
        get => _weights;
        set => _weights = value;
    }

    public LinearNeuron(int weightCount, double bias, double learningRate)
    {
        _bias = bias;
        _learningRate = learningRate;
        _weights = new double[weightCount];
    }

    public INeuron FillRandomValues(Random random, double range = 4)
    {
        for (int i = 0; i < _weights.Length; i++)
            _weights[i] = random.NextDouble() * range - range;

        return this;
    }

    public double Predict(double[] input)
    {
        if (_weights.Length != input.Length)
            throw new InvalidOperationException();
        
        double sum = 0;

        for (int i = 0; i < input.Length; i++)
            sum += _weights[i] * input[i];
        
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
