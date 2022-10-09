using MLL.Common.Net;
using MLL.Neurons;

namespace MLL.Statistics;

public interface IStatisticsManager
{
    void CollectStats(int epoch, NetManager net);
    void AddOutputError(ReadOnlySpan<float> error);
}
