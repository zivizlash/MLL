using Microsoft.Extensions.Configuration;
using MLL.Common.Builders.Computers;
using MLL.Common.Builders.Weights;
using MLL.Common.Engines;
using MLL.Common.Files;
using MLL.Common.Manager;
using MLL.Common.Optimization;
using MLL.Common.Tools;
using MLL.Computers.Factory;
using MLL.Computers.Factory.Defines;
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
