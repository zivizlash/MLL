using LiteDB;
using Microsoft.Extensions.Configuration;
using MLL.Common.Builders.Computers;
using MLL.Common.Builders.Weights;
using MLL.Common.Files;
using MLL.Common.Layer;
using MLL.Common.Net;
using MLL.Common.Optimization;
using MLL.Computers.Factory;
using MLL.Computers.Factory.Defines;
using MLL.CUI.Options;
using MLL.Files.ImageLoader;
using MLL.Statistics.Collection;
using MLL.Statistics.Collection.Processors;
using MLL.ThreadingOptimization;

namespace MLL.CUI;

public class Program
{
    public static void Main()
    {
        var args = ArgumentParser.GetArguments();
        var options = ImageRecognitionOptions.Default;

        var layers = GetLayersDefinition(options);
        var weights = layers.ToWeights().ToArray();

        RandomFill(weights, options);

        var computers = CreateComputers();
        var net = new NetManager(computers.Computers, weights, new(computers.Collectors));

        var netMethods = new NetMethods(net, options.LearningRate);

        if (args.Train)
        {
            var trainSet = CreateDataSetProvider(true, options);
            var trainComputers = CreateComputers(false);

            var trainTestNet = new NetManager(trainComputers.Computers,
                layers.ToWeights(), new(trainComputers.Collectors));

            var (netSaver, statSaver, stats) = CreateStatisticsManager(
                400, CreateDataSetProvider(false, options), trainSet, trainTestNet);

            netMethods.Train(trainSet, stats);

            netSaver.Save(net);
            statSaver.WriteLayers(layers);
            statSaver.Flush();
        }

        if (!args.CheckRecognition)
        {
            netMethods.FullTest(CreateDataSetProvider(false, options));
        }
    }

    private static (NetSaver, StatisticsSaver, StatisticsManager) CreateStatisticsManager(
        int delimmer, IImageDataSetProvider test, IImageDataSetProvider train, NetManager computers)
    {
        var statCalc = new StatisticsCalculator(test, train);

        var netSaver = new NetSaver(delimmer);
        var statSaver = new StatisticsSaver();
        var statConsoleWriter = new StatisticsConsoleWriter();

        var processors = new IStatProcessor[] { statConsoleWriter, statSaver, netSaver };
        var statsManager = new StatisticsManager(statCalc, processors, delimmer, computers);

        return (netSaver, statSaver, statsManager);
    }

    private static LayerComputerBuilderResult CreateComputers(bool forTrain = true)
    {
        var settings = new ThreadingOptimizatorFactorySettings(100000, 0.2f, 3);
        var optimizatorFactory = new ThreadingOptimizatorFactory(settings);
        var computerFactory = new BasicLayerComputerFactory(optimizatorFactory);

        return new LayerComputerBuilder(computerFactory)
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SigmoidLayerDefine>()
            .UseLayer<SumLayerDefine>()
            .Build(forTrain);
    }

    private static void RandomFill(LayerWeights[] weights, ImageRecognitionOptions options)
    {
        var rnd = new Random(options.RandomSeed!.Value);

        foreach (var neuron in weights.SelectMany(w => w.Weights))
        {
            for (int i = 0; i < neuron.Length; i++)
            {
                neuron[i] = rnd.NextSingle() * 2 - 1;
            }
        }
    }

    private static LayerWeightsDefinition[] GetLayersDefinition(ImageRecognitionOptions options) =>
        LayerWeightsDefinition.Builder
            .WithInputLayer(10 * 3, options.ImageWidth * options.ImageHeight)
            .WithLayer(10 * 2)
            .WithLayer(10)
            .Build();

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

    private static IImageDataSetProvider CreateDataSetProvider(bool isEven, ImageRecognitionOptions options) =>
        new FolderNameDataSetProvider(new NotOrEvenFilesProviderFactory(isEven, 512),
            NameToFolder, options.ImageWidth, options.ImageHeight);

    private static string NameToFolder(string name) =>
        $"C:\\Auto\\datasets\\Font\\Font\\Sample{int.Parse(name) + 1:d3}";
}
