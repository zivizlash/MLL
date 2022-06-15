using MLL.Builders;
using MLL.ImageLoader;
using MLL.Neurons;
using MLL.Options;
using Newtonsoft.Json;

namespace MLL.Statistics;

public class StatContainer<T>
{
    public int Epoch { get; }
    public T Value { get; }

    public StatContainer(int epoch, T value)
    {
        Epoch = epoch;
        Value = value;
    }
}

public class NeuronErrorStats
{
    public float[] Errors { get; }

    public NeuronErrorStats(float[] errors)
    {
        Errors = errors;
    }
}

public interface IStatisticsManager
{
    void CollectStats(int epoch, Net net);
    void CollectOutputError(Net net);
}

public class StatisticsManager : IStatisticsManager
{
    private readonly string _workingDirectory;
    private readonly Dictionary<string, int> _counter;

    private readonly IImageDataSetProvider _testSetProvider;
    private readonly IImageDataSetProvider _trainSetProvider;

    private float[]? _outputErrors;
    private int _epoch;

    public StatisticsManager(ImageRecognitionOptions recognitionOptions, LayerDefinition[] layers,
        IImageDataSetProvider testSetProvider, IImageDataSetProvider trainSetProvider)
    {
        _testSetProvider = testSetProvider;
        _trainSetProvider = trainSetProvider;
        _counter = new Dictionary<string, int>();
        _workingDirectory = Directory.CreateDirectory(GetFullPath()).FullName;

        WriteOptions(recognitionOptions);
        WriteLayers(layers);
    }

    public void CollectOutputError(Net net)
    {
        var outputErrors = net.Buffers.GetLastLayerBufferRaw();

        if (_outputErrors == null)
            _outputErrors = new float[outputErrors.Length];

        for (var i = 0; i < outputErrors.Length; i++)
        {
            var outputError = outputErrors[i];
            _outputErrors[i] += Math.Abs(outputError);
        }
    }

    public void CollectStats(int epoch, Net net)
    {
        _epoch = epoch;

        if (epoch % 5 == 0)
        {
            if (epoch % 50 == 0)
                WriteNetCopy(net);

            var testRecognized = Create(net, _testSetProvider, true);
            var trainRecognized = Create(net, _trainSetProvider, false);

            Write("test_recognize", testRecognized);
            Write("train_recognize", trainRecognized);
            
            Write("train_errors", new NeuronErrorStats(_outputErrors!));

            Array.Fill(_outputErrors!, 0f);
        }
    }
    
    private NeuronRecognizedStats Create(Net net, IImageDataSetProvider provider, bool isTest)
    {
        var buffer = new float[10];
        RecognitionPercentCalculator.Calculate(net, provider, buffer);
        return new NeuronRecognizedStats(buffer, buffer.Sum() / 10.0f, isTest);
    }

    private void WriteNetCopy(Net net)
    {
        Write("net.json", net);
    }
    
    private void WriteOptions(ImageRecognitionOptions options)
    {
        WriteAndSerialize("net_options.json", options);
    }

    private void WriteLayers(LayerDefinition[] layers)
    {
        WriteAndSerialize("layers", layers);
    }

    private int GetCounterValueAndIncrement(string value)
    {
        if (_counter.TryGetValue(value, out var val))
        {
            _counter[value]++;
            return val;
        }

        _counter[value] = 1;
        return 0;
    }

    private void Write<T>(string name, T obj)
    {
        var counter = GetCounterValueAndIncrement(name);
        WriteAndSerialize($"{name}{counter}.json", new StatContainer<T>(_epoch, obj));
    }

    private void WriteAndSerialize(string filename, object obj) =>
        WriteFile(filename, JsonConvert.SerializeObject(obj));

    private void WriteFile(string filename, string content) =>
        File.WriteAllText(GetNewFilePath(filename), content);

    private string GetNewFilePath(string filename) =>
        Path.Combine(_workingDirectory, filename);

    private static string GetFullPath() =>
        Path.Combine(Environment.CurrentDirectory, "..", "..", "Stats", GetDirectoryName());

    private static string GetDirectoryName() =>
        $"{DateTime.Now:yyyy.M.d hh-mm-ss}";
}
