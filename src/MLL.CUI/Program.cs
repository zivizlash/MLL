using LiteDB;
using Microsoft.Extensions.Configuration;
using MLL.Common.Builders.Computers;
using MLL.Common.Builders.Weights;
using MLL.Common.Files;
using MLL.Common.Net;
using MLL.Common.Optimization;
using MLL.Computers.Factory;
using MLL.Computers.Factory.Defines;
using MLL.CUI.Options;
using MLL.Files.ImageLoader;
using MLL.Files.Tools;
using MLL.Statistics.Collection;
using MLL.Statistics.Collection.Processors;
using MLL.ThreadingOptimization;

namespace MLL.CUI;

public class Program
{
    private static IImageDataSetProvider CreateDataSetProvider(bool isEven) =>
        new FolderNameDataSetProvider(new NotOrEvenFilesProviderFactory(isEven, 128),
            NameToFolder, ImageRecognitionOptions.Default.ImageWidth, ImageRecognitionOptions.Default.ImageHeight);

    private static IImageDataSetProvider CreateTestDataSetProvider() => CreateDataSetProvider(false);
    private static IImageDataSetProvider CreateTrainDataSetProvider() => CreateDataSetProvider(true);

    private static LayerDefinition[] GetLayersDefinition(ImageRecognitionOptions options)
    {
        const int numbersCount = 10;
        var imageWeightsCount = options.ImageWidth * options.ImageHeight;

        return new[] { LayerDefinition.CreateSingle(numbersCount, imageWeightsCount) }; 

        //return LayerDefinition.Builder
        //    .WithLearningRate(options.LearningRate)
        //    .WithInputLayer(numbersCount * 3, imageWeightsCount)
        //    .WithHiddenLayers(numbersCount * 2)
        //    .WithOutputLayer(numbersCount, false)
        //    .Build();
    }

    private static (NetSaver, StatisticsSaver, StatisticsManager) CreateStatisticsManager(
        int delimmer, IImageDataSetProvider test, IImageDataSetProvider train, NetManager computers)
    {
        var statCalc = new StatisticsCalculator(test, train);

        var netSaver = new NetSaver();
        var statSaver = new StatisticsSaver();
        var statConsoleWriter = new StatisticsConsoleWriter();

        var processors = new IStatProcessor[]
        {
            statConsoleWriter, statSaver, netSaver
        };

        var statsManager = new StatisticsManager(statCalc, processors, delimmer, computers);
        return (netSaver, statSaver, statsManager);
    }

    private static LayerComputerBuilderResult CreateNeuronComputers(bool forTrain = true)
    {
        var settings = new ThreadingOptimizatorFactorySettings(100000, 0.2f, Environment.ProcessorCount);
        var computerFactory = new BasicLayerComputerFactory(new ThreadingOptimizatorFactory(settings));

        return new LayerComputerBuilder(computerFactory)
            .UseLayer<SumLayerDefine>()
            .Build(forTrain);
    }

    public static void Main()
    {
        var args = ArgumentParser.GetArguments();
        var imageOptions = ImageRecognitionOptions.Default;

        var layers = GetLayersDefinition(imageOptions);
        var weights = layers.ToWeights().ToArray();

        var random = new Random(imageOptions.RandomSeed!.Value);

        foreach (var neuron in weights.SelectMany(w => w.Neurons))
        {
            for (int i = 0; i < neuron.Length; i++)
            {
                neuron[i] = random.NextSingle() * 2 - 1;
            }
        }

        var computers = CreateNeuronComputers();

        var net = new NetManager(computers.Computers.ToArray(), weights, 
            new OptimizationManager(computers.Collectors));

        var netMethods = new NetMethods(net, imageOptions.LearningRate);
        var testDataSet = CreateTestDataSetProvider();

        if (args.Train)
        {
            var trainDataSet = CreateTrainDataSetProvider();
            var trainTestComputers = CreateNeuronComputers(false);

            var trainTestNet = new NetManager(
                trainTestComputers.Computers.ToArray(), 
                layers.ToWeights().ToArray(), 
                new OptimizationManager(trainTestComputers.Collectors));

            var (netSaver, statSaver, stats) = CreateStatisticsManager(
                200, testDataSet, trainDataSet, trainTestNet);

            netMethods.Train(trainDataSet, stats);

            netSaver.Save(net);
            statSaver.WriteLayers(layers);
            statSaver.Flush();
        }

        if (!args.CheckRecognition && !args.TestImageNormalizing)
        {
            netMethods.FullTest(testDataSet);
        }

        if (args.TestImageNormalizing)
        {
            ImageTools.TestImageNormalizing();
        }
    }

    private static IConfiguration CreateConfiguration() =>
        new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

    private static string NameToFolder(string name) =>
        $"C:\\Auto\\datasets\\Font\\Font\\Sample{int.Parse(name) + 1:d3}";
}
