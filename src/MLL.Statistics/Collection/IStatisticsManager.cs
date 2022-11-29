using MLL.Common.Net;

namespace MLL.Statistics.Collection;

public interface IStatisticsManager
{
    void CollectStats(int epoch, Net net);
    void AddOutputError(ReadOnlySpan<float> error);
    void Flush();
}
