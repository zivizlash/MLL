using ILGPU;
using ILGPU.Runtime.Cuda;
using Microsoft.Extensions.Configuration;
using MLL.Common.Builders.Computers;
using MLL.Common.Builders.Weights;
using MLL.Common.Engines;
using MLL.Common.Files;
using MLL.Common.Layer;
using MLL.Common.Manager;
using MLL.Common.Optimization;
using MLL.Common.Tools;
using MLL.Computers.Factory;
using MLL.Computers.Factory.Defines;
using MLL.Computers.Layers.Backpropagation;
using MLL.Computers.Layers.Sigmoid;
using MLL.Computers.Layers.Sum;
using MLL.Cuda;
using MLL.CUI.Options;
using MLL.Files.Tools;
using MLL.Repository;
using MLL.Repository.Factory;
using MLL.Repository.Tools;
using MLL.Statistics.Collection;
using MLL.Statistics.Collection.Processors;
using MLL.ThreadingOptimization;
using Newtonsoft.Json;

namespace MLL.CUI;

public readonly struct LayerBaseInfo
{
    public readonly int? NeuronsCount;
    public readonly int? WeightsCount;

    public LayerBaseInfo(int? neuronsCount, int? weightsCount)
    {
        NeuronsCount = neuronsCount;
        WeightsCount = weightsCount;
    }
}

public interface ILayerFactory
{
    LayerComputers CreateComputers();
    LayerBaseInfo BaseInfo { get; }
}

public static class LayerSettingsBagExtensions
{
    public static LayerSettingsBag SetupThreading(this LayerSettingsBag settingsBag)
    {
        //return settingsBag
        //    .Add(new );

        return settingsBag;
    }
}

public class LayerSettingsBag
{
    private readonly Dictionary<Type, object> _typesToObjects;

    public LayerSettingsBag Add(object arg)
    {
        _typesToObjects.Add(arg.GetType(), arg);
        return this;
    }

    public LayerSettingsBag()
    {
        _typesToObjects = new Dictionary<Type, object>();
    }
}

public readonly struct LayerFactoryContext
{

}

public interface ILayerBuilder
{
    ILayerBuilder AddLayer(ILayerFactory factory);
}

public interface IInputLayerBuilder
{
    ILayerBuilder AddInputLayer(int neuronsCount, Action<ILayerBuilder> builder);
}

public static class SigmoidNetBuilderExtensions
{
    public static ILayerBuilder UseSigmoid(this ILayerBuilder builder)
    {
        return UseSigmoid(builder, _ => { });
    }

    public static ILayerBuilder UseSigmoid(this ILayerBuilder builder, Action<SigmoidLayerSettings> setup)
    {
        var settings = new SigmoidLayerSettings();
        setup.Invoke(settings);

        return builder.AddLayer(new SigmoidFactory
        {
            NeuronsCount = settings.NeuronsCount
        });
    }

    private class SigmoidFactory : ILayerFactory
    {
        public LayerBaseInfo BaseInfo => new(NeuronsCount, WeightsCount);

        public int? NeuronsCount { get; set; }
        public int? WeightsCount { get; set; }

        public LayerComputers CreateComputers()
        {
            return new LayerComputers(new CommonErrorComputer(), new SigmoidPredictComputer(),
                new SigmoidCompensateComputer(), new ThreadedErrorBackpropagation());
        }
    }
}

public class SigmoidLayerSettings
{
    internal int? NeuronsCount { get; set; }
    internal int? WeightsCount { get; set; }

    public SigmoidLayerSettings Neurons(int count)
    {
        NeuronsCount = count;
        return this;
    }

    public SigmoidLayerSettings Weights(int count)
    {
        WeightsCount = count;
        return this;
    }
}

public abstract class AdvancedNetFactory
{
    protected abstract void Configure(ILayerBuilder builder);

    public void Construct()
    {
        var builder = new FactoryLayerBuilder();
        Configure(builder);
    }

    private class FactoryLayerBuilder : ILayerBuilder, IInputLayerBuilder
    {
        public List<ILayerFactory> Factories { get; } = new();

        public ILayerBuilder AddInputLayer(int neuronsCount, Action<ILayerBuilder> builder)
        {
            builder.Invoke(this);
            return this;
        }

        public ILayerBuilder AddLayer(ILayerFactory factory)
        {
            Factories.Add(factory);
            return this;
        }
    }
}

public class TestAdvancedFactory : AdvancedNetFactory
{
    protected override void Configure(ILayerBuilder builder) => 
        builder
            .UseSigmoid(s => s.Weights(100).Neurons(10))
            .UseSigmoid(s => s.Neurons(10))
            .UseSigmoid(s => s.Neurons(20));
}

public class RaceImageRecognitionNetBuilder : NetInfoFillerNetFactory
{
    public RaceImageRecognitionNetBuilder(INetInfo netInfo, ImageRecognitionOptions options) 
        : base(netInfo, options.RandomSeed ?? 0)
    {
    }

    public override LayerComputerBuilderResult GetComputers(bool isForTrain)
    {
        var settings = new ThreadingOptimizatorFactorySettings(1000, 0.3f, 12);
        var optimizatorFactory = new ThreadingOptimizatorFactory(settings);
        var computerFactory = new BasicLayerComputerFactory(optimizatorFactory);

        return new LayerComputerBuilder(computerFactory)
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SumLayerDefine>()
            .Build(isForTrain);
    }

    public override LayerWeightsDefinition[] GetDefinitions() =>
        LayerWeightsDefinition.Builder
            .WithInputLayer(24 * 32, 240 * 320 + 1)
            .WithLayer(100)
            .WithLayer(100)
            .WithLayer(2)
            .Build();
}

public class Program
{
    private static void Main()
    {
        // Execute();

        using var context = Context.Create(ctx => ctx.AllAccelerators());

        var device = context.GetPreferredDevice(preferCPU: false);
        using var accelerator = device.CreateAccelerator(context);

        var computer = new VectorCudaComputer(accelerator);

        var weights = new float[4, 16];

        var input = Enumerable.Range(0, 16).Select(i => (float)i).ToArray();
        var localOutput = new float[weights.GetLength(0)];

        for (int wi = 0; wi < weights.GetLength(0); wi++)
        {
            for (int ii = 0; ii < weights.GetLength(1); ii++)
            {
                weights[wi, ii] = wi + ii;
            }
        }

        for (int x = 0; x < weights.GetLength(0); x++)
        {
            for (int y = 0; y < weights.GetLength(1); y++)
            {
                localOutput[x] += weights[x, y] * input[y];
            }
        }

        computer.Prepare(weights, input);

        computer.Execute();

        var output = new float[localOutput.Length];

        computer.CopyOutput(output);

        //computer.Execute(weights, input);

        Console.WriteLine($"Local output: {string.Join(", ", localOutput)}");
        Console.WriteLine($"Output: {string.Join(", ", output)}");
    }

    private static void Execute()
    {
        int delimmer = 100;

        var args = ArgumentParser.GetArguments();
        //var args = new ArgumentParser(true, false, true);

        var configuration = CreateConfiguration();
        var netOptions = configuration.GetSection(nameof(NetOptions)).Get<NetOptions>();
        var netInfo = GetNetInfo(netOptions);

        var options = ImageRecognitionOptions.Default;
        var factory = new RaceImageRecognitionNetBuilder(netInfo, options);

        var net = factory.Create(forTrain: true);
        var epochStats = netInfo.Data.GetOrNew<EpochNetStats>();

        var netManager = new NetManager(net, options.LearningRate, epochStats.Epoch);
        var testSet = CreateSetDataProvider(isEven: false);

        if (args.Train)
        {
            var testNet = factory.Create(forTrain: false);
            var trainSet = CreateSetDataProvider(isEven: true);
            var stats = CreateStatisticsManager(delimmer, testSet, testNet, netInfo);

            netManager.Train(trainSet, stats, ArgumentParser.IsExitRequested);
            stats.Flush();
        }

        if (args.CheckRecognition)
        {
            netManager.FullTest(testSet);
        }
    }

    private static INetInfo GetNetInfo(NetOptions options)
    {
        var folder = string.IsNullOrEmpty(options.DataFolder) 
            ? Environment.CurrentDirectory 
            : options.DataFolder;

        return new NetDatabase(folder).OpenOrCreate(new("Race game frame recognition"), netInfo =>
        {
            netInfo.Description = "Recognize rgb pixels to distance to wall";
        });
    }

    private static StatisticsManager CreateStatisticsManager(int delimmer, 
        IDataSetProvider test, ClassificationEngine net, INetInfo netInfo)
    {
        var statCalc = new StatisticsCalculator(test);

        var statSaver = new StatisticsSaver(netInfo, netInfo.Data);
        var statConsoleWriter = new StatisticsConsoleWriter();

        var processors = new IStatProcessor[] { statConsoleWriter, statSaver };
        return new StatisticsManager(statCalc, processors, delimmer, net);
    }
    
    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

    private static IDataSetProvider CreateSetDataProvider(bool isEven) =>
        new JsonDataSetProvider("C:\\Auto\\screenshots", isEven);
}

public class JsonDataSetProvider : IDataSetProvider
{
    private readonly string _folder;
    private readonly bool _isEven;
    private readonly Info[] _infos;
    private IDataSet[]? _imageDataSet;

    public JsonDataSetProvider(string folder, bool isEven = false)
    {
        _folder = folder;
        _isEven = isEven;

        var json = File.ReadAllText(Path.Combine(folder, "info.json"));
        _infos = JsonConvert.DeserializeObject<Info[]>(json) ?? throw new InvalidOperationException();
    }

    public IDataSet[] GetDataSets()
    {
        var counter = _isEven ? 0 : 1;

        return _imageDataSet ??= _infos
            .Where(_ => counter++ % 2 == 0)
            .Select(info =>
            {
                var value = new float[] { info.LeftDistance, info.RightDistance };
                var options = new ImageDataSetOptions(240, 320);

                var sourceImageData = ImageTools.LoadImageData(Path.Combine(_folder, info.FileName), options);
                var imageData = new float[sourceImageData.Length + 1];

                Array.Copy(sourceImageData, 0, imageData, 0, sourceImageData.Length);
                imageData[^1] = 1;

                return new FileImageDataSet(options, value, new SetData(imageData));
            })
            .ToArray();
    }

    public class FileImageDataSet : IDataSet
    {
        public SetData this[int index]
        {
            get
            {
                if (index != 0)
                {
                    Throw.ArgumentOutOfRange(nameof(index));
                }

                return _image;
            }
        }

        private readonly SetData _image;

        public float[] Value { get; }

        public int Count => 1;

        public ImageDataSetOptions Options { get; }

        public FileImageDataSet(ImageDataSetOptions options, float[] value, SetData imageData)
        {
            Options = options;
            Value = value;
            _image = imageData;
        }
    }

    public class Info
    {
        public string FileName { get; set; } = string.Empty;
        public float LeftDistance { get; set; }
        public float RightDistance { get; set; }
    }
}
