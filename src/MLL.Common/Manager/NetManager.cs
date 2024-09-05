using MLL.Common.Engines;
using MLL.Common.Files;
using MLL.Common.Statistics;
using MLL.Common.Tools;

namespace MLL.Common.Manager;

public class NetManager
{
    private readonly ClassificationEngine _net;
    private readonly float _learningRate;
    private readonly int _epoch;

    public NetManager(ClassificationEngine net, float learningRate, int epoch)
    {
        _net = net;
        _learningRate = learningRate;
        _epoch = epoch;
    }

    private static void PrepareTraining(IDataSetProvider imageProvider)
    {
        Console.WriteLine("Loading and process images...");
        imageProvider.GetDataSets();
        Console.WriteLine("Images loaded\n");
    }

    public void Train(IDataSetProvider imageProvider, IStatisticsManager stats, Func<bool> isExitRequired)
    {
        var dt = DateTime.Now;
        void DisplayTrainTime() => Console.WriteLine($"Training ended in {DateTime.Now - dt}\n");

        PrepareTraining(imageProvider);

        int epoch = _epoch;
        var random = new Random();

        while (!isExitRequired.Invoke())
        {
            ProcessEpoch(epoch++, imageProvider, stats, random);
        }

        Console.WriteLine($"Train stopped at {epoch} epoch");
        DisplayTrainTime();
    }

    private void ProcessEpoch(int epoch, IDataSetProvider imageProvider, IStatisticsManager stats, Random random)
    {
        var dataSets = imageProvider.GetDataSets();
        dataSets.ShuffleInPlace(random);

        foreach (var dataSet in dataSets)
        {
            for (int i = 0; i < dataSet.Count; i++)
            {
                var image = dataSet[i];

                var errors = _net.Train(image.Data, dataSet.Value, _learningRate);
                stats.AddOutputError(errors);
            }
        }

        stats.CollectStats(epoch, _net);
    }

    public void FullTest(IDataSetProvider imageProvider)
    {
        static string ConvertFloatToString(float value) => value.ToString("F3").Replace(",", ".").PadLeft(6);

        foreach (var dataSet in imageProvider.GetDataSets())
        {
            var expected = dataSet.Value;
            var stringExpected = expected.Select(ConvertFloatToString);

            for (int i = 0; i < dataSet.Count; i++)
            {
                var data = dataSet[i].Data;
                var result = _net.Predict(data).ToArray();

                var offsets = expected
                    .Zip(result, (first, second) => new { First = first, Second = second })
                    .Select(v => Math.Abs(v.Second - v.First));

                Console.WriteLine(
                    $"Expected: [{string.Join(", ", expected.Select(ConvertFloatToString))}]; " +
                    $"Actual: [{string.Join(", ", result.Select(ConvertFloatToString))}]; " +
                    $"Offset: [{string.Join(", ", offsets.Select(ConvertFloatToString))}]");
            }
        }
    }
}
