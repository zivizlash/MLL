using MLL.Repository;

namespace MLL.Statistics.Collection.Processors;

public class EpochNetStats
{
    public int Epoch { get; set; }
}

public class StatisticsSaver : IStatProcessor
{
    private readonly INetInfo _netInfo;
    private readonly INetData _globalData;

    private int _lastEpoch;

    public StatisticsSaver(INetInfo netInfo, INetData globalData)
    {
        _netInfo = netInfo;
        _globalData = globalData;
    }

    public void Process(StatisticsInfo stats)
    {
        var data = _netInfo.AddSnapshot(stats.Net.Weights).Data;
        data.Set(stats.TestStats);
        data.Set(stats.ErrorStats);
        data.Set(new EpochNetStats { Epoch = stats.EpochRange.End });
        _lastEpoch = stats.EpochRange.End;
    }

    public void Flush()
    {
        _globalData.Set(new EpochNetStats { Epoch = _lastEpoch });
    }
}
