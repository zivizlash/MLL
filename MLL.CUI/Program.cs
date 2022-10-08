using LiteDB;
using Microsoft.Extensions.Configuration;
using MLL.Builders;
using MLL.CUI;
using MLL.ImageLoader;
using MLL.Layer;
using MLL.Layer.Factories;
using MLL.Layer.Threading;
using MLL.Neurons;
using MLL.Options;
using MLL.Statistics;
using MLL.Statistics.Processors;
using MLL.Tools;

namespace MLL;

public static class DefinitionToWeights
{ 
    public static IEnumerable<LayerWeightsData> ToWeights(this IEnumerable<LayerDefinition> defs)
    {
        foreach (var def in defs)
        {
            var neurons = new float[def.NeuronsCount][];

            for (int i = 0; i < neurons.Length; i++)
            {
                neurons[i] = new float[def.WeightsCount];
            }

            yield return new LayerWeightsData(neurons);
        }
    }
}

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

        return new []{ LayerDefinition.CreateSingle(numbersCount, imageWeightsCount, false) }; 

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
        return new LayerComputerBuilder()
            .MaxThreadsAsProccessorsCount()
            .RequiredSamples(10000)
            .OutlinersThreshold(0.2f)
            .UseLayer<SumLayerDef>()
            .Build(forTrain);
    }

    public static void Main()
    {
        var computers = CreateNeuronComputers();

        var args = ArgumentParser.GetArguments();
        var imageOptions = ImageRecognitionOptions.Default;

        var layers = GetLayersDefinition(imageOptions);

        var weights = layers.ToWeights().ToArray();

        var random = new Random(imageOptions.RandomSeed!.Value);

        foreach (var w in weights)
        {
            foreach (var neuron in w.Neurons)
            {
                for (int i = 0; i < neuron.Length; i++)
                    neuron[i] = random.NextSingle() * 2 - 1;
            }
        }

        var net = new NetManager(computers.Computers.ToArray(), weights, 
            new OptimizationManager(computers.Collectors.ToArray()));

        var netMethods = new NetMethods(net, imageOptions.LearningRate);
        var testDataSet = CreateTestDataSetProvider();

        if (args.Train)
        {
            var trainDataSet = CreateTrainDataSetProvider();
            var trainTestComputers = CreateNeuronComputers(false);

            var trainTestNet = new NetManager(trainTestComputers.Computers.ToArray(), 
                layers.ToWeights().ToArray(), new OptimizationManager(trainTestComputers.Collectors.ToArray()));

            var (netSaver, statSaver, stats) = CreateStatisticsManager(200, testDataSet, trainDataSet, trainTestNet);

            netMethods.Train(trainDataSet, stats);

            netSaver.Save(net);
            statSaver.WriteLayers(layers);
            statSaver.WriteOptions(imageOptions);
            statSaver.Flush();
        }

        if (!args.CheckRecognition && !args.TestImageNormalizing)
            netMethods.FullTest(testDataSet);

        if (args.TestImageNormalizing)
            ImageTools.TestImageNormalizing();
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
