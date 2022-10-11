using MLL.Common.Layer;
using MLL.Common.Net;

namespace MLL.Statistics.Collection;

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

    public void Flush()
    {
        foreach (var processor in _processors)
        {
            processor.Flush();
        }
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
