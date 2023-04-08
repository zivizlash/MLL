using MLL.Common.Tools;

namespace MLL.Computers.Layers.Common.WorkInfo;

public readonly struct ErrorWorkInfo
{
    public readonly float[] Outputs;
    public readonly float[] Expected;
    public readonly float[] Errors;
    public readonly ProcessingRange ProcessingRange;
    public readonly CountdownEvent? Countdown;

    public ErrorWorkInfo(float[] outputs, float[] expected, float[] errors,
        ProcessingRange processingRange, CountdownEvent? countdown)
    {
        Outputs = outputs;
        Expected = expected;
        Errors = errors;
        ProcessingRange = processingRange;
        Countdown = countdown;
    }
}
