using MLL.Tools;

namespace MLL.Layer.Computers;

public class SigmoidLayerCompensateWorkItem : IHasExecuteDelegate
{
    public LayerWeightsData Layer;
    public float[] Input;
    public float LearningRate;
    public float[] Errors;
    public float[] Outputs;
    public int ProcessingCount;
    public int Index;
    public CountdownEvent Countdown;

    public WaitCallback ExecuteDelegate { get; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public SigmoidLayerCompensateWorkItem()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        ExecuteDelegate = Execute;
    }

    public void Execute(object? _)
    {
        var (start, end) = ThreadTools.Loop(ProcessingCount, Index);

        var neurons = Layer.Neurons;

        for (int ni = start; ni < end; ni++)
        {
            var weights = neurons[ni];
            var generalError = GetGeneralError(LearningRate, Outputs[ni], Errors[ni]);

            for (int wi = 0; wi < weights.Length; wi++)
            {
                weights[wi] += generalError * Input[wi];
            }
        }

        Countdown.Signal();
    }

    private static float GetGeneralError(float learningRate, float output, float error)
    {
        float sigmoidDerivative = NumberTools.SigmoidDerivative(output);
        return learningRate * error * sigmoidDerivative;
    }
}
