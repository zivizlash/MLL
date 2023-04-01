using MLL.Common.Engines;

namespace MLL.Statistics.Collection.Processors;

public class NetSaver : IStatProcessor
{
    private readonly string _filePath;

    public NetSaver(string filePath)
    {
        _filePath = filePath;
    }

    public bool TryLoad(out NetWeights weights)
    {
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            weights = Newtonsoft.Json.JsonConvert.DeserializeObject<NetWeights>(json);
            return true;
        }

        weights = default;
        return false;
    }

    public void Process(StatisticsInfo stats)
    {
        Save(stats.Net);
    }

    public void Flush()
    {
    }

    public void Save(ClassificationEngine net)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(net.Weights.Layers);
        File.WriteAllText(_filePath, json);
    }
}
