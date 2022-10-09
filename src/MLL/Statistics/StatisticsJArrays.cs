using Newtonsoft.Json.Linq;

namespace MLL.Statistics;

public struct StatisticsJArrays
{
    public JArray Net;
    public JArray TestRecognize;
    public JArray TrainRecognize;
    public JArray TrainErrors;

    public StatisticsJArrays()
    {
        Net = new JArray();
        TestRecognize = new JArray();
        TrainRecognize = new JArray();
        TrainErrors = new JArray();
    }
}
