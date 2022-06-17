using MLL.Builders;
using MLL.ImageLoader;
using MLL.Neurons;
using MLL.Options;
using MLL.Statistics.Processors;
using Newtonsoft.Json.Linq;

namespace MLL.Statistics;

public struct StatisticsJArrays
{
    public JArray Net;
    public JArray TestRecognize;
    public JArray TrainRecognize;
    public JArray TrainErrors;

    public StatisticsJArrays()
    {
        Net = new JArray();
        TestRecognize = new JArray();
        TrainRecognize = new JArray();
        TrainErrors = new JArray();
    }
}

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
    
    public StatisticsInfo Calculate(Net net, Range epochRange)
    {
        NormalizeErrorPerEpoch(_outputErrors!, epochRange);
        var testRecognized = Recognize(net, _testSetProvider, true);
        var trainRecognized = Recognize(net, _trainSetProvider, false);
        var trainErrors = new NeuronErrorStats(_outputErrors!);
        
        return new StatisticsInfo(testRecognized, trainRecognized, 
            trainErrors, epochRange, net);
    }

    public void Clear()
    {
        Array.Clear(_outputErrors!);
    }

    public void AddOutputError(float[] error)
    {
        _outputErrors ??= new float[error.Length];

        for (var i = 0; i < error.Length; i++)
            _outputErrors[i] += Math.Abs(error[i]);
    }

    private static void NormalizeErrorPerEpoch(float[] errors, Range epochRange)
    {
        var epochCount = GetDelta(epochRange);
        if (epochCount == 0) return;

        for (int i = 0; i < errors.Length; i++)
            errors[i] /= epochCount;
    }

    // Мне пофиг, я так чувствую
    private static int GetDelta(Range range) 
    {
        return Math.Abs(range.End.Value) - Math.Abs(range.Start.Value);
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

public interface IStatisticsManager
{
    void CollectStats(int epoch, Net net);
    void AddOutputError(float[] error);
}

public class StatisticsManager : IStatisticsManager
{
    private readonly StatisticsCalculator _calculator;
    private readonly IStatProcessor[] _processors;

    private readonly object _locker = new();

    private Net? _netCopy;

    private int _delimmer = 20;

    public StatisticsManager(StatisticsCalculator calculator, IStatProcessor[] processors, int delimmer)
    {
        _calculator = calculator;
        _processors = processors;
        _delimmer = delimmer;
    }

    public void AddOutputError(float[] error)
    {
        _calculator.AddOutputError(error);
    }

    public void CollectStats(int epoch, Net net)
    {
        if (epoch % _delimmer != 0 || epoch == 0)
            return;
        
        var localCopy = _netCopy;
        NetReplicator.Copy(net, ref localCopy);

        var container = new StatContainer<Net>(epoch, localCopy);
        _netCopy = localCopy;

        ThreadPool.QueueUserWorkItem(Process, container, false);
    }

    private void Process(StatContainer<Net> net)
    {
        lock (_locker)
        {
            var epoch = (net.Epoch - _delimmer)..net.Epoch;
            var stats = _calculator.Calculate(net.Value, epoch);

            foreach (var processor in _processors)
                processor.Process(stats);

            _calculator.Clear();
        }
    }
}
