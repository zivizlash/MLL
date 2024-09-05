using MLL.Common.Engines;
using MLL.Common.Layer;
using MLL.Repository.Data;
using MLL.Repository.Tools;
using Newtonsoft.Json;

namespace MLL.Repository;

public class NetSnapshot : INetSnapshot
{
    private readonly string _folder;

    private DirectoryInfo? _dir;
    private INetData? _data;

    private DirectoryInfo Dir => _dir ??= Directory.CreateDirectory(_folder);

    public INetData Data => _data ??= new NetDataFolder(
        Path.Combine(Dir.FullName, "settings")).WithCacheAndDateTimeUpdate();

    public NetWeights Weights
    { 
        get => GetWeights();
        set => SaveWeights(value);
    }

    public NetSnapshot(string folder)
    {
        _folder = folder;
    }

    private NetWeights GetWeights()
    {
        var serializer = new JsonSerializer();

        try
        {
            var stream = File.OpenText(Path.Combine(Dir.FullName, "weights.json"));
            using var jsonReader = new JsonTextReader(stream);

            var value = serializer.Deserialize<WritableNet>(jsonReader);

            if (value == null)
            {
                return default;
            }

            var weights = new LayerWeights[value.Layers.Length];

            for (int i = 0; i < value.Layers.Length; i++)
            {
                weights[i] = new LayerWeights(value.Layers[i].Weights);
            }

            return new NetWeights(weights);
        }
        catch (FileNotFoundException)
        {
            return default;
        }
    }

    public void SaveWeights(NetWeights weights)
    {
        var serializer = new JsonSerializer();
        var filePath = Path.Combine(Dir.FullName, "weights.json");

        using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        using var textWriter = new StreamWriter(fileStream);
        fileStream.Position = 0;

        serializer.Serialize(textWriter, weights);
        fileStream.SetLength(fileStream.Position);
    }

    private class WritableNet
    {
        public WritableWeights[] Layers = Array.Empty<WritableWeights>();
    }

    private class WritableWeights
    {
        public float[][] Weights = Array.Empty<float[]>();
    }
}
