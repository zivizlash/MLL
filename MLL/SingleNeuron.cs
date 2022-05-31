using System.Numerics;

namespace MLL;

public interface INeuron
{
    public double LastError { get; }
    public double[] Weights { get; set; }

    INeuron FillRandomValues(Random random, double range = 4);
    double Predict(double[] input);
    double Train(double[] input, double expected);
}

public abstract class Neuron : INeuron
{
    public double[] Weights { get; set; }
    public double LearningRate { get; set; }

    public double LastError { get; protected set; }

    protected Neuron(int weightCount, double learningRate)
    {
        LearningRate = learningRate;
        Weights = new double[weightCount];
    }

    public abstract double Predict(double[] input);
    public abstract double Train(double[] input, double expected);

    protected double GetWeightMultiplySum(double[] input)
    {
        if (Weights.Length != input.Length)
            throw new InvalidOperationException();
        
        var vectorSize = Vector<double>.Count;
        var accVector = Vector<double>.Zero;

        int i;

        for (i = 0; i < Weights.Length - vectorSize; i += vectorSize)
        {
            var v1 = new Vector<double>(Weights, i);
            var v2 = new Vector<double>(input, i);
            accVector = Vector.Add(accVector, Vector.Multiply(v1, v2));
        }

        double sum = Vector.Sum(accVector);

        for (; i < Weights.Length; i++)
            sum += Weights[i] * input[i];

        return sum;
    }

    public INeuron FillRandomValues(Random random, double range = 1)
    {
        for (int i = 0; i < Weights.Length; i++)
            Weights[i] = random.NextDouble() * range - range;

        return this;
    }
}

public class SigmoidNeuron : Neuron
{
    public SigmoidNeuron(int weightCount, double learningRate)
        : base(weightCount, learningRate)
    {
    }
    
    public override double Predict(double[] input)
    {
        var sum = GetWeightMultiplySum(input);
        return NumberTools.Sigmoid(sum);
    }

    public override double Train(double[] input, double expected)
    {
        var output = Predict(input);
        var error = -(expected - output);

        for (int i = 0; i < Weights.Length; i++)
        {
            var delta = LearningRate * error * output * (1.0 - output) * input[i];
            Weights[i] -= delta;
        }

        return error;
    }
}

public class SingleNeuron : Neuron
{
    private readonly double _bias;

    public SingleNeuron(int weightCount, double bias, double learningRate)
        : base(weightCount, learningRate)
    {
        _bias = bias;
    }
    
    public override double Predict(double[] input)
    {
        var sum = GetWeightMultiplySum(input);
        LastError = _bias - sum;
        return sum >= _bias ? 1 : 0;
    }

    public override double Train(double[] input, double expected)
    {
        var rawError = expected - Predict(input);
        var error = rawError;
        
        for (int i = 0; i < input.Length; i++)
            Weights[i] += error * input[i] * LearningRate;

        return error;
    }
}
