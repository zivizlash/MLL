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
        int delimmer = 200;

        var args = ArgumentParser.GetArguments();
        var options = ImageRecognitionOptions.Default;

        var layers = GetLayersDefinition(options);
        var weights = layers.ToWeights().ToArray().RandomFill(options.RandomSeed!.Value);
        
        var net = CreateNetManager(CreateComputers(), weights);
        var netMethods = new NetMethods(net, options.LearningRate);

        if (args.Train)
        {
            var trainSet = CreateDataSetProvider(true, options);
            var testSet = CreateDataSetProvider(false, options);

            var testNet = CreateNetManager(CreateComputers(false), layers.ToWeights());
            var stats = CreateStatisticsManager(delimmer, testSet, trainSet, testNet, layers);

            netMethods.Train(trainSet, stats);
            stats.Flush();
        }

        if (!args.CheckRecognition)
        {
            netMethods.FullTest(CreateDataSetProvider(false, options));
        }
    }

    private static StatisticsManager CreateStatisticsManager(
        int delimmer, IImageDataSetProvider test, IImageDataSetProvider train, 
        NetManager net, LayerWeightsDefinition[] defines)
    {
        var statCalc = new StatisticsCalculator(test, train);

        var netSaver = new NetSaver(delimmer);
        var statSaver = new StatisticsSaver(defines);
        var statConsoleWriter = new StatisticsConsoleWriter();

        var processors = new IStatProcessor[] { statConsoleWriter, statSaver, netSaver };
        return new StatisticsManager(statCalc, processors, delimmer, net);
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

    private static NetManager CreateNetManager(LayerComputerBuilderResult result, IEnumerable<LayerWeights> weights) =>
        new(result.Computers, weights, new OptimizationManager(result.Collectors));

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
