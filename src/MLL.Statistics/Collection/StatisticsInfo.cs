using MLL.Common.Net;

namespace MLL.Statistics.Collection;

public class StatisticsInfo
{
    public NeuronRecognizedStats TestStats { get; }
    public NeuronRecognizedStats TrainStats { get; }
    public NeuronErrorStats ErrorStats { get; }
    public Net Net { get; }
    public EpochRange EpochRange { get; }

    public StatisticsInfo(NeuronRecognizedStats testStats, NeuronRecognizedStats trainStats,
        NeuronErrorStats errorStats, EpochRange epochRange, Net net)
    {
        TestStats = testStats;
        TrainStats = trainStats;
        ErrorStats = errorStats;
        EpochRange = epochRange;
        Net = net;
    }
}
