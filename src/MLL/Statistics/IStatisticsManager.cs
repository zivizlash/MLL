using MLL.Common.Net;

namespace MLL.Statistics;

public interface IStatisticsManager
{
    void CollectStats(int epoch, NetManager net);
    void AddOutputError(ReadOnlySpan<float> error);
}
