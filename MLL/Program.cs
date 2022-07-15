using MLL.Builders;
using MLL.ImageLoader;
using MLL.Neurons;
using MLL.Options;
using MLL.Saving;
using MLL.Statistics;
using MLL.Statistics.Processors;
using MLL.Tools;

namespace MLL;

public class Program
{
    private static Random GetRandomBySeed(int? seed) =>
        seed.HasValue ? new Random(seed.Value) : new Random();
    
    private static Net GetNet(bool loadFromDisk, ImageRecognitionOptions options, LayerDefinition[] layers)
    {
        var net = loadFromDisk
            ? NeuronWeightsSaver.Load()
            : CreateWithHiddenLayers(options, layers);

        return net.UpdateLearningRate(options.LearningRate);
    }

    private static IImageDataSetProvider CreateDataSetProvider(bool isEven) =>
        new FolderNameDataSetProvider(new NotOrEvenFilesProviderFactory(isEven, 512),
            NameToFolder, ImageRecognitionOptions.Default.ImageWidth, ImageRecognitionOptions.Default.ImageHeight);

    private static IImageDataSetProvider CreateTestDataSetProvider() => CreateDataSetProvider(false);
    private static IImageDataSetProvider CreateTrainDataSetProvider() => CreateDataSetProvider(true);

    private static Net CreateWithHiddenLayers(ImageRecognitionOptions options, LayerDefinition[] layers) =>
        new Net(options.LearningRate, layers).FillRandomValues(GetRandomBySeed(options.RandomSeed), 1);

    private static LayerDefinition[] GetLayersDefinition(ImageRecognitionOptions options)
    {
        const int numbersCount = 10;
        var imageWeightsCount = options.ImageWidth * options.ImageHeight;

        return new []{ LayerDefinition.CreateSingle(numbersCount, imageWeightsCount, false) }; 

        return LayerDefinition.Builder
            .WithLearningRate(options.LearningRate)
            .WithInputLayer(numbersCount * 3, imageWeightsCount)
            .WithHiddenLayers(numbersCount * 2)
            .WithOutputLayer(numbersCount, false)
            .Build();
    }

    private static (NetSaver, StatisticsSaver, StatisticsManager) CreateStatisticsManager(
        int delimmer, IImageDataSetProvider test, IImageDataSetProvider train)
    {
        var calculator = new StatisticsCalculator(test, train);

        var netSaver = new NetSaver();
        var statSaver = new StatisticsSaver();

        var processors = new IStatProcessor[]
        {
            new StatisticsConsoleWriter(),
            statSaver,
            netSaver
        };

        var statsManager = new StatisticsManager(calculator, processors, delimmer);
        return (netSaver, statSaver, statsManager);
    }

    public static void Main()
    {
        var args = ArgumentParser.GetArguments();
        var imageOptions = ImageRecognitionOptions.Default;

        var layers = GetLayersDefinition(imageOptions);
        var net = GetNet(args.LoadFromDisk, imageOptions, layers);
        
        var netMethods = new NetMethods(net);
        var testDataSet = CreateTestDataSetProvider();

        if (args.Train)
        {
            var trainDataSet = CreateTrainDataSetProvider();
            var (netSaver, statSaver, stats) = CreateStatisticsManager(400, testDataSet, trainDataSet);

            netMethods.Train(trainDataSet, stats);

            netSaver.Save(net);
            statSaver.WriteLayers(layers);
            statSaver.WriteOptions(imageOptions);
            statSaver.Flush();
        }

        if (!args.CheckRecognition && !args.TestImageNormalizing)
            netMethods.FullTest(testDataSet);

        if (args.CheckRecognition)
            throw new NotImplementedException(); // netMethods.CheckRecognition();
        
        if (args.TestImageNormalizing)
            ImageTools.TestImageNormalizing();
    }

    private static string NameToFolder(string name) =>
        $"C:\\Auto\\datasets\\Font\\Font\\Sample{int.Parse(name) + 1:d3}";
}
