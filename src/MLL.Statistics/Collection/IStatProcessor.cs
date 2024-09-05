namespace MLL.Statistics.Collection;

public interface IStatProcessor
{
    void Process(StatisticsInfo stats);
    void Flush();
}
