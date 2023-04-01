using MLL.Common.Engines;
using MLL.Common.Files;
using MLL.Common.Tools;
using MLL.Statistics.Collection;
using MLL.Statistics.Collection.Processors;

namespace MLL.CUI;

public class NetManager
{
    private readonly ClassificationEngine _net;
    private readonly float _learningRate;
    private readonly EpochNetStats _epoch;
    private readonly NeuronErrorStats _error;

    public NetManager(ClassificationEngine net, float learningRate, EpochNetStats epoch, NeuronErrorStats error)
    {
        _net = net;
        _learningRate = learningRate;
        _epoch = epoch;
        _error = error;
    }

    private static void PrepareTraining(IImageDataSetProvider imageProvider)
    {
        Console.WriteLine("Loading and process images...");
        imageProvider.GetDataSets();
        Console.WriteLine("Images loaded\n");
    }

    public void Train(IImageDataSetProvider imageProvider, IStatisticsManager stats)
    {
        var dt = DateTime.Now;
        void DisplayTrainTime() => Console.WriteLine($"Training ended in {DateTime.Now - dt}\n");

        PrepareTraining(imageProvider);

        int epoch = _epoch.Epoch;
        var random = new Random();

        while (!ArgumentParser.IsExitRequested())
        {
            ProcessEpoch(epoch++, imageProvider, stats, random);
        }

        Console.WriteLine($"Train stopped at {epoch} epoch");
        DisplayTrainTime();
    }

    private void ProcessEpoch(int epoch, IImageDataSetProvider imageProvider, IStatisticsManager stats, Random random)
    {
        var dataSets = imageProvider.GetDataSets();
        dataSets.ShuffleInPlace(random);

        foreach (var dataSet in dataSets)
        {
            for (int i = 0; i < dataSet.Count; i++)
            {
                var image = dataSet[i];
                var expected = (float[])image.Value;

                var errors = _net.Train(image.Data, expected, _learningRate);
                stats.AddOutputError(errors);
            }
        }

        stats.CollectStats(epoch, _net);
    }

    public void FullTest(IImageDataSetProvider imageProvider)
    {
        static string ConvertFloatToString(float value) => 
            value.ToString("F3").Replace(",", ".").PadLeft(6);

        foreach (var dataSet in imageProvider.GetDataSets())
        {
            var expected = (float[])dataSet.Value;
            var stringExpected = expected.Select(ConvertFloatToString);

            for (int i = 0; i < dataSet.Count; i++)
            {
                var data = dataSet[i].Data;
                var result = _net.Predict(data).ToArray();

                var offsets = expected.Zip(result).Select(v => Math.Abs(v.Second - v.First));

                Console.WriteLine(
                    $"Expected: [{string.Join(", ", expected.Select(ConvertFloatToString))}]; " +
                    $"Actual: [{string.Join(", ", result.Select(ConvertFloatToString))}]; " +
                    $"Offset: [{string.Join(", ", offsets.Select(ConvertFloatToString))}]");
            }
        }
    }
}
