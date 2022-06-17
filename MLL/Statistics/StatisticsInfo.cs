using MLL.Neurons;

namespace MLL.Statistics;

public class StatisticsInfo
{
    public NeuronRecognizedStats TestStats { get; }
    public NeuronRecognizedStats TrainStats { get; }
    public NeuronErrorStats ErrorStats { get; }
    public Net Net { get; }
    public Range EpochRange { get; }

    public StatisticsInfo(NeuronRecognizedStats testStats, NeuronRecognizedStats trainStats, 
        NeuronErrorStats errorStats, Range epochRange, Net net)
    {
        TestStats = testStats;
        TrainStats = trainStats;
        ErrorStats = errorStats;
        EpochRange = epochRange;
        Net = net;
    }
}
