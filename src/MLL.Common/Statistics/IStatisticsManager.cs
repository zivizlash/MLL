using MLL.Common.Engines;

namespace MLL.Common.Statistics;

public interface IStatisticsManager
{
    void CollectStats(int epoch, ClassificationEngine net);
    void AddOutputError(ReadOnlySpan<float> error);
    void Flush();
}
