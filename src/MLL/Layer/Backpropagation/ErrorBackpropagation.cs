using MLL.Common.Layer.Backpropagation;
using MLL.Common.Tools;
using MLL.Tools;

namespace MLL.Layer.Backpropagation;

public class ErrorBackpropagation : IErrorBackpropagation
{
    public void ReorganizeErrors(BackpropContext ctx, float[] errors)
    {
        Check.LengthEqual(ctx.Neurons[0].Length, errors.Length, nameof(errors));
        Array.Clear(errors);

        var prevNeurons = ctx.Neurons;
        var prevErrors = ctx.Errors;

        for (int neuronIndex = 0; neuronIndex < ctx.Neurons.Length; neuronIndex++)
        {
            var weights = prevNeurons[neuronIndex];
            var error = prevErrors[neuronIndex];
            ErrorBackpropagationTools.CalculateNeuronError(weights, error, errors);
        }
    }
}
