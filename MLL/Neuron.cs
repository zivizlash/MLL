using System.Numerics;

namespace MLL;

public abstract class Neuron
{
    public float[] Weights { get; set; }
    public float LearningRate { get; set; }

    public float LastError { get; protected set; }

    protected Neuron(int weightCount, float learningRate)
    {
        LearningRate = learningRate;
        Weights = new float[weightCount];
    }

    public abstract float Predict(float[] input);
    public abstract float Train(float[] input, float expected);

    public float CalculateWeightsSum()
    {
        var vectorSize = Vector<float>.Count;
        var accVector = Vector<float>.Zero;

        int i;

        for (i = 0; i < Weights.Length - vectorSize; i += vectorSize)
        {
            var v1 = new Vector<float>(Weights, i);
            accVector = Vector.Add(accVector, Vector.Abs(v1));
        }

        float sum = Vector.Sum(accVector);

        for (; i < Weights.Length; i++)
            sum += MathF.Abs(Weights[i]);

        return sum;
    }
    
    protected float CalculateWeightMultiplySum(float[] input)
    {
        if (Weights.Length != input.Length)
            throw new InvalidOperationException();
        
        var vectorSize = Vector<float>.Count;
        var accVector = Vector<float>.Zero;

        int i;
        
        for (i = 0; i < Weights.Length - vectorSize; i += vectorSize)
        {
            var v1 = new Vector<float>(Weights, i);
            var v2 = new Vector<float>(input, i);
            accVector = Vector.Add(accVector, Vector.Multiply(v1, v2));
        }

        float sum = Vector.Sum(accVector);

        for (; i < Weights.Length; i++)
            sum += Weights[i] * input[i];

        return sum;
    }

    public Neuron FillRandomValues(Random random, double range = 1)
    {
        for (int i = 0; i < Weights.Length; i++)
            Weights[i] = (random.NextSingle() - 0.5f) * (float)range;

        return this;
    }
}
