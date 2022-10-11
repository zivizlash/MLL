using MLL.Common.Net;

namespace MLL.Statistics.Collection.Processors;

public class NetSaver : IStatProcessor
{
    private readonly int _delimmer;

    public NetSaver(int delimmer = 200)
    {
        _delimmer = delimmer;
    }

    public void Process(StatisticsInfo stats)
    {
        if (stats.EpochRange.End % _delimmer == 0)
        {
            Save(stats.Net);
        }
    }

    public void Save(NetManager net)
    {
        //NeuronWeightsSaver.Save(net);
    }
}
