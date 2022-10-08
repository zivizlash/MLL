namespace MLL.Neurons;

// на память тупа. сколько времени я потратил на усложнение поддержки

//public class SingleNeuron : Neuron
//{
//    private readonly double _bias;

//    public SingleNeuron(int weightCount, double bias, double learningRate)
//        : base(weightCount, learningRate)
//    {
//        _bias = bias;
//    }

//    public override double Predict(double[] input)
//    {
//        var sum = CalculateWeightMultiplySum(input);
//        LastError = _bias - sum;
//        return sum >= _bias ? 1 : 0;
//    }

//    public override double Train(double[] input, double expected)
//    {
//        var rawError = expected - Predict(input);
//        var error = rawError;

//        for (int i = 0; i < input.Length; i++)
//            Weights[i] += error * input[i] * LearningRate;

//        return error;
//    }
//}
