using MLL.Tools;

namespace MLL.Neurons;

public sealed class SigmoidNeuron : Neuron
{
    private bool _useActivationFunc;

    public bool UseActivationFunc
    {
        get => _useActivationFunc;
        set => _useActivationFunc = value;
    }

    public SigmoidNeuron(int weightCount, float learningRate, bool useActivationFunc)
        : base(weightCount, learningRate)
    {
        _useActivationFunc = useActivationFunc;
    }

    public override float Predict(float[] input)
    {
        float sum = CalculateWeightMultiplySum(input);

        return _useActivationFunc
            ? NumberTools.Sigmoid(sum)
            : sum;
    }

    public override void CompensateError(float[] input, NeuronError neuronError)
    {
        var commonDeltaWeight = GetDeltaWeight(neuronError.Output, neuronError.Error);

        for (int i = 0; i < Weights.Length; i++)
            Weights[i] -= commonDeltaWeight * input[i];
    }

    public override NeuronError CalculateError(float[] input, float expected)
    {
        var output = Predict(input);
        var error = -(expected - output);
        return new NeuronError(error, output);
    }

    public override float Train(float[] input, float expected)
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

    public override SigmoidNeuron Clone()
    {
        var copy = new SigmoidNeuron(Weights.Length, LearningRate, UseActivationFunc);
        Weights.CopyTo(copy.Weights.AsSpan());
        return copy;
    }

    private float GetDeltaWeight(float output, float error)
    {
        if (_useActivationFunc)
        {
            float sigmoid = output * (1.0f - output);
            return LearningRate * error * sigmoid;
        }

        return LearningRate * error;
    }
}
