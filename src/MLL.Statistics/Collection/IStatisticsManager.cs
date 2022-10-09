using MLL.Common.Net;

namespace MLL.Statistics.Collection;

public interface IStatisticsManager
{
    void CollectStats(int epoch, NetManager net);
    void AddOutputError(ReadOnlySpan<float> error);
}
