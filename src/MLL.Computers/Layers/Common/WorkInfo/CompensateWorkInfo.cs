using MLL.Common.Layer;
using MLL.Computers.Tools;

namespace MLL.Computers.Layers.Common.WorkInfo;

public readonly struct CompensateWorkInfo
{
    public readonly LayerWeights Layer;
    public readonly float[] Input;
    public readonly float[] Errors;
    public readonly float[] Outputs;
    public readonly float LearningRate;
    public readonly ProcessingRange ProcessingRange;
    public readonly CountdownEvent? Countdown;

    public CompensateWorkInfo(LayerWeights layer, float[] input, float[] errors, float[] outputs,
        float learningRate, ProcessingRange processingRange, CountdownEvent? countdown)
    {
        Layer = layer;
        Input = input;
        LearningRate = learningRate;
        Errors = errors;
        Outputs = outputs;
        ProcessingRange = processingRange;
        Countdown = countdown;
    }
}
