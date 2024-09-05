using MLL.Common.Layer;
using MLL.Common.Tools;

namespace MLL.Computers.Layers.Common.WorkInfo;

public readonly struct PredictWorkInfo
{
    public readonly LayerWeights Layer;
    public readonly float[] Input;
    public readonly float[] Results;
    public readonly ProcessingRange ProcessingRange;
    public readonly CountdownEvent? Countdown;

    public PredictWorkInfo(LayerWeights layer, float[] input, float[] results,
        ProcessingRange processingRange, CountdownEvent? countdown)
    {
        Layer = layer;
        Input = input;
        Results = results;
        ProcessingRange = processingRange;
        Countdown = countdown;
    }
}
