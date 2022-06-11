using MLL.ImageLoader;
using MLL.Neurons;
using MLL.Saving;

namespace MLL;

public class NetMethods
{
    private readonly Net _net;
    private readonly float[][] _expectedValues;

    public NetMethods(Net net)
    {
        _net = net;
        
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
        Console.WriteLine("Images loaded");
    }

    public void Train(IImageDataSetProvider imageProvider)
    {
        var dt = DateTime.Now;
        PrepareTraining(imageProvider);

        var count = imageProvider.GetDataSet(0).Count;

        for (int epoch = 0; epoch < 2000; epoch++)
        {
            float errorAcc = 0;

            for (int imageIndex = 0; imageIndex < count; imageIndex++)
            {
                for (int imageNumber = 0; imageNumber < 10; imageNumber++)
                {
                    var imagesSet = imageProvider.GetDataSet(imageNumber);
                    var expected = _expectedValues[imageNumber];

                    var image = imagesSet[imageIndex];
                    var errors = _net.Train(image.Data, expected);
                    foreach (var error in errors) errorAcc += MathF.Abs(error);
                }
            }
            
            Console.WriteLine($"Epoch {epoch:D4} Error: {errorAcc:F10}");

            if (epoch % 20 == 0)
            {
                FullTest(imageProvider);
                NeuronWeightsSaver.Save(_net);
            }
        }

        Console.WriteLine($"Training ended in {DateTime.Now - dt}\n");
    }

    public void FullTest(IImageDataSetProvider imageProvider)
    {
        float recognizedPercents = 0;
    
        for (int i = 0; i < 10; i++)
            recognizedPercents += Test2(imageProvider.GetDataSet(i));

        Console.WriteLine();
        Console.WriteLine($"Overall recognized percents: {recognizedPercents / 10.0f}");
    }

    public float Test2(IImageDataSet imageSet)
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

        Console.WriteLine($"{imageSet.Value}: {successString}");
        return successPercents;
    }
}
