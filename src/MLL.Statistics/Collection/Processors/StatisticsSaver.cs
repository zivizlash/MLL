using MLL.Common.Builders.Weights;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MLL.Statistics.Collection.Processors;

public class StatisticsSaver : IStatProcessor
{
    private readonly string _workingDirectory;
    private readonly StatisticsJArrays _stats;

    public StatisticsSaver()
    {
        _workingDirectory = Directory.CreateDirectory(GetFullPath()).FullName;
        _stats = new();
    }

    public void WriteLayers(LayerWeightsDefinition[] layers) =>
        WriteAndSerialize("layers.json", layers);

    public void Process(StatisticsInfo stats)
    {
        int epoch = stats.EpochRange.Start;

        if (epoch % 100 == 0)
            _stats.Net.Add(ToArrayElement(epoch, stats.Net));

        _stats.TestRecognize.Add(ToArrayElement(epoch, stats.TestStats));
        _stats.TrainRecognize.Add(ToArrayElement(epoch, stats.TrainStats));
        _stats.TrainErrors.Add(ToArrayElement(epoch, stats.ErrorStats));
    }

    public void Flush()
    {
        WriteContent("net.json", _stats.Net.ToString());
        WriteContent("train.json", _stats.TrainRecognize.ToString());
        WriteContent("test.json", _stats.TestRecognize.ToString());
        WriteContent("errors.json", _stats.TrainErrors.ToString());
    }

    private void WriteAndSerialize(string filename, object obj)
    {
        var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
        WriteContent(filename, json);
    }

    private void WriteContent(string filename, string content)
    {
        var filepath = Path.Combine(_workingDirectory, filename);
        File.WriteAllText(filepath, content);
    }

    private static JObject ToArrayElement<T>(int epoch, T val)
    {
        var container = new StatContainer<T>(epoch, val);
        return JObject.FromObject(container);
    }

    private static string GetFullPath()
    {
        var directoryName = $"{DateTime.Now:yyyy.M.d hh-mm-ss}";
        var current = Environment.CurrentDirectory;
        return Path.Combine(current, "..", "..", "Stats", directoryName);
    }
}
