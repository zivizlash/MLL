using MLL.Common.Files;
using MLL.Common.Net;
using MLL.CUI;
using MLL.Files.ImageLoader;
using MLL.Statistics.Collection;

namespace MLL;

public class NetMethods
{
    private readonly Net _net;
    private readonly float _learningRate;
    private readonly float[][] _expectedValues;

    public NetMethods(Net net, float learningRate)
    {
        _net = net;
        _learningRate = learningRate;
        _expectedValues = Enumerable
            .Range(0, 10).Select(v =>
            {
                var values = new float[10];
                values[v] = 1;
                return values;
            }).ToArray();
    }

    private static void PrepareTraining(IImageDataSetProvider imageProvider)
    {
        var keys = Enumerable.Range(0, 10).ToList();
        var count = imageProvider.GetLargestImageDataSetCount(keys);
        ImageDataSetProviderExtensions.EnsureKeys(count);

        Console.WriteLine("Loading and process images...");
        imageProvider.LoadAllImages(keys);
        Console.WriteLine("Images loaded\n");
    }

    public void Train(IImageDataSetProvider imageProvider, IStatisticsManager stats)
    {
        var dt = DateTime.Now;
        void DisplayTrainTime() => Console.WriteLine($"Training ended in {DateTime.Now - dt}\n");

        PrepareTraining(imageProvider);

        var numbers = Enumerable.Range(0, 10);

        var indices = numbers.SelectMany(number => 
            Enumerable.Range(0, imageProvider.GetDataSet(number).Count)
                .Select(index => (number, index)));

        var indicesShuffler = new IndicesShuffler(indices);
        int epoch = 0;

        while (!ArgumentParser.IsExitRequested())
        {
            ProcessEpoch(epoch++, indicesShuffler, imageProvider, stats);
        }

        Console.WriteLine($"Train stopped at {epoch} epoch");
        DisplayTrainTime();
    }

    private void ProcessEpoch(int epoch, IndicesShuffler indices, IImageDataSetProvider imageProvider, IStatisticsManager stats)
    {
        foreach (var (imageNumber, imageIndex) in indices.ShuffleAndGet())
        {
            var image = imageProvider.GetDataSet(imageNumber)[imageIndex];
            var expected = _expectedValues[imageNumber];

            var errors = _net.Train(image.Data, expected, _learningRate);
            stats.AddOutputError(errors);
        }

        stats.CollectStats(epoch, _net);
    }

    public float FullTest(IImageDataSetProvider imageProvider, float? previous = default)
    {
        float recognizedPercents = 0;
    
        for (int i = 0; i < 10; i++)
        {
            recognizedPercents += Test(imageProvider.GetDataSet(i));
            if (i == 4) Console.WriteLine();
        }

        Console.WriteLine();
        Console.Write($"Overall recognized percents: {recognizedPercents / 10.0f};");

        if (previous.HasValue)
            Console.WriteLine($" Delta: {recognizedPercents - previous.Value};");
        
        Console.WriteLine();
        return recognizedPercents;
    }

    public float Test(IImageDataSet imageSet)
    {
        var error = 0;
        
        for (int i = 0; i < imageSet.Count; i++)
        {
            var imageData = imageSet[i];
            var results = _net.Predict(imageData.Data);
            
            var max = results[0];
            var index = 0;

            for (int resultIndex = 1; resultIndex < results.Length; resultIndex++)
            {
                var value = results[resultIndex];

                if (value > max)
                {
                    max = value;
                    index = resultIndex;
                }
            }

            if (!index.Equals(imageSet.Value))
                error++;
        }

        var successPercents = (1.0f - error / (float)imageSet.Count) * 100;
        var successString = successPercents.ToString("F3").Replace(',', '.');

        Console.Write($"{imageSet.Value}: {successString}; ");
        return successPercents;
    }
}
