using MLL.Common.Files;
using MLL.Common.Net;

namespace MLL.Statistics.Collection;

public class StatisticsCalculator
{
    private readonly IImageDataSetProvider _testSetProvider;
    private readonly IImageDataSetProvider _trainSetProvider;

    private float[]? _outputErrors;

    public StatisticsCalculator(IImageDataSetProvider testSetProvider,
        IImageDataSetProvider trainSetProvider)
    {
        _testSetProvider = testSetProvider;
        _trainSetProvider = trainSetProvider;
    }

    public StatisticsInfo Calculate(Net net, EpochRange epochRange)
    {
        NormalizeErrorPerEpoch(_outputErrors!, epochRange);
        var testRecognized = Recognize(net, _testSetProvider, true);
        var trainRecognized = Recognize(net, _trainSetProvider, false);
        var trainErrors = new NeuronErrorStats(_outputErrors!);

        return new StatisticsInfo(testRecognized, trainRecognized, trainErrors, epochRange, net);
    }

    public void Clear()
    {
        Array.Clear(_outputErrors!);
    }

    public void AddOutputError(ReadOnlySpan<float> error)
    {
        _outputErrors ??= new float[error.Length];

        for (var i = 0; i < error.Length; i++)
            _outputErrors[i] += Math.Abs(error[i]);
    }

    private static void NormalizeErrorPerEpoch(float[] errors, EpochRange epochRange)
    {
        var epochCount = GetDelta(epochRange);
        if (epochCount == 0) return;

        for (int i = 0; i < errors.Length; i++)
            errors[i] /= epochCount;
    }

    private static int GetDelta(EpochRange range)
    {
        return range.End - range.Start;
    }

    private static NeuronRecognizedStats Recognize(
        Net net, IImageDataSetProvider provider, bool isTest)
    {
        var results = new float[10];
        RecognitionPercentCalculator.Calculate(net, provider, results);
        var general = results.Sum() / 10.0f;

        return new NeuronRecognizedStats(results, general, isTest);
    }
}
