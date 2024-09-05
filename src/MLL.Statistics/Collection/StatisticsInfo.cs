using MLL.Common.Engines;

namespace MLL.Statistics.Collection;

public class StatisticsInfo
{
    public NeuronRecognizedStats TestStats { get; }
    public NeuronErrorStats ErrorStats { get; }
    public ClassificationEngine Net { get; }
    public EpochRange EpochRange { get; }

    public StatisticsInfo(NeuronRecognizedStats testStats, NeuronErrorStats errorStats, 
        EpochRange epochRange, ClassificationEngine net)
    {
        TestStats = testStats;
        ErrorStats = errorStats;
        EpochRange = epochRange;
        Net = net;
    }
}
