using System.Numerics;

namespace MLL;

public abstract class Neuron
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

    public double CalculateWeightsSum()
    {
        var vectorSize = Vector<double>.Count;
        var accVector = Vector<double>.Zero;

        int i;

        for (i = 0; i < Weights.Length - vectorSize; i += vectorSize)
        {
            var v1 = new Vector<double>(Weights, i);
            accVector = Vector.Add(accVector, Vector.Abs(v1));
        }

        double sum = Vector.Sum(accVector);

        for (; i < Weights.Length; i++)
            sum += Math.Abs(Weights[i]);

        return sum;
    }
    
    protected double CalculateWeightMultiplySum(double[] input)
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

    public Neuron FillRandomValues(Random random, double range = 1)
    {
        for (int i = 0; i < Weights.Length; i++)
            Weights[i] = random.NextDouble() * range - range;

        return this;
    }
}
