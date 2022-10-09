using MLL.Common.Net;
using MLL.Neurons;

namespace MLL.Statistics.Processors;

public class NetSaver : IStatProcessor
{
    public void Process(StatisticsInfo stats)
    {
        if (stats.EpochRange.End % 200 == 0)
            Save(stats.Net);
    }

    public void Save(NetManager net)
    {
        // NeuronWeightsSaver.Save(net);
    }
}
