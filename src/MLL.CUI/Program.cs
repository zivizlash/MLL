using Microsoft.Extensions.Configuration;
using MLL.Common.Branching;
using MLL.Common.Builders.Computers;
using MLL.Common.Builders.Weights;
using MLL.Common.Engines;
using MLL.Common.Factory;
using MLL.Common.Files;
using MLL.Common.Layer;
using MLL.Common.Optimization;
using MLL.Common.Tools;
using MLL.Computers.Factory;
using MLL.Computers.Factory.Defines;
using MLL.CUI.Options;
using MLL.Files.ImageLoader;
using MLL.Statistics.Collection;
using MLL.Statistics.Collection.Processors;
using MLL.ThreadingOptimization;
using System.Text;

namespace MLL.CUI;

internal class BranchesNetFactory : RandomFillNetFactory
{
    private readonly ImageRecognitionOptions _options;

    public BranchesNetFactory(ImageRecognitionOptions options) : base(options.RandomSeed ?? 0)
    {
        _options = options;
    }

    public override LayerComputerBuilderResult GetComputers(bool isForTrain)
    {
        var settings = new ThreadingOptimizatorFactorySettings(100000, 0.2f, 8);
        var optimizatorFactory = new ThreadingOptimizatorFactory(settings);
        var computerFactory = new BasicLayerComputerFactory(optimizatorFactory);

        return new LayerComputerBuilder(computerFactory)
            //.UseLayer<SigmoidLayerDefine>()
            //.UseLayer<SigmoidLayerDefine>()
            .UseLayer<SumLayerDefine>()
            .Build(isForTrain);
    }

    public override LayerWeightsDefinition[] GetDefinitions()
    {
        return LayerWeightsDefinition.Builder
            .WithInputLayer(10, _options.ImageWidth * _options.ImageHeight)
            //.WithLayer(10 * 2)
            //.WithLayer(10)
            .Build();
    }
}

public class Program
{
    private static void RandomTest()
    {
        var options = ImageRecognitionOptions.Default;
        var factory = new BranchesNetFactory(options);

        var netBranches = new NetBranches(10, new(0.005f), factory);

        var layers = factory.GetDefinitions();

        var trainSet = CreateDataSetProvider(true, options);
        var testSet = CreateDataSetProvider(false, options);

        const int delimmer = 4000;

        var testNet = CreateNetManager(factory.GetComputers(false), layers.ToWeights());
        var stats = CreateStatisticsManager(delimmer, testSet, trainSet, testNet, layers);

        var dataSets = Enumerable.Range(0, 10).Select(trainSet.GetDataSet).ToArray();

        int epoch = 0;

        while (!ArgumentParser.IsExitRequested())
        {
            var results = netBranches.Train(dataSets);
            stats.AddOutputError(results);
            stats.CollectStats(epoch, netBranches.RefNet);

            netBranches.Optimize();

            if (epoch % delimmer == 0)
            {
                var st = new StringBuilder();

                var result = netBranches.RefNet.Predict(dataSets.First()[0].Data);
                st.AppendLine("Recognized 0 by neurons: ");

                for (int i = 0; i < result.Length; i++)
                {
                    if (i == 5) st.AppendLine();
                    st.Append($"{i}: {result[i]}; ");
                }

                st.AppendLine().AppendLine();
                Console.Write(st.ToString());
            }

            epoch++;
        }

        stats.Flush();
    }

    private static void Main()
    {
        RandomTest();
        return;

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
        ClassificationEngine net, LayerWeightsDefinition[] defines)
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

    private static ClassificationEngine CreateNetManager(LayerComputerBuilderResult result, IEnumerable<LayerWeights> weights) =>
        new ClassificationEngine(result.Computers, weights, 
            new OptimizationManager(result.Collectors), 
            NetLayersBuffers.CreateByWeights(weights));

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
        new FolderNameDataSetProvider(new NotOrEvenFilesProviderFactory(isEven, 64),
            NameToFolder, options.ImageWidth, options.ImageHeight);

    private static string NameToFolder(string name) =>
        $"C:\\Auto\\datasets\\Font\\Font\\Sample{int.Parse(name) + 1:d3}";
}
