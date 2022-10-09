using MLL.ImageLoader;
using MLL.Layer;
using MLL.Neurons;
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
    
    public StatisticsInfo Calculate(NetManager net, EpochRange epochRange)
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
        NetManager net, IImageDataSetProvider provider, bool isTest)
    {
        var results = new float[10];
        RecognitionPercentCalculator.Calculate(net, provider, results);
        var general = results.Sum() / 10.0f;

        return new NeuronRecognizedStats(results, general, isTest);
    }
}

public interface IStatisticsManager
{
    void CollectStats(int epoch, NetManager net);
    void AddOutputError(ReadOnlySpan<float> error);
}

public struct EpochRange
{
    public int Start { get; set; }
    public int End { get; set; }

    public EpochRange(int start, int end)
    {
        Start = start;
        End = end;
    }
}

public class StatisticsManager : IStatisticsManager
{
    private readonly StatisticsCalculator _calculator;
    private readonly IStatProcessor[] _processors;

    private readonly object _locker = new();
    private readonly NetManager _computers;

    private LayerWeights[]? _netCopy;

    private int _delimmer = 20;

    public StatisticsManager(StatisticsCalculator calculator, IStatProcessor[] processors, 
        int delimmer, NetManager computers)
    {
        _calculator = calculator;
        _processors = processors;
        _delimmer = delimmer;
        _computers = computers;
    }
    
    public void AddOutputError(ReadOnlySpan<float> error)
    {
        _calculator.AddOutputError(error);
    }

    public void CollectStats(int epoch, NetManager net)
    {
        if (epoch % _delimmer != 0) return;
        
        var localCopy = _netCopy;
        var copy = NetReplicator.Copy(net, _computers, ref localCopy);

        var container = new StatContainer<NetManager>(epoch, copy);
        _netCopy = localCopy;

        ThreadPool.QueueUserWorkItem(Process, container, false);
    }

    private void Process(StatContainer<NetManager> net)
    {
        lock (_locker)
        {
             var epoch = net.Epoch != 0
                ? new EpochRange(net.Epoch - _delimmer, net.Epoch)
                : new EpochRange();

            var stats = _calculator.Calculate(net.Value, epoch);

            foreach (var processor in _processors)
            {
                processor.Process(stats);
            }

            _calculator.Clear();
        }
    }
}
