using MLL.Common.Engines;

namespace MLL.Statistics.Collection;

public interface IStatisticsManager
{
    void CollectStats(int epoch, ClassificationEngine net);
    void AddOutputError(ReadOnlySpan<float> error);
    void Flush();
}
