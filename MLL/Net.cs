using System.Runtime.Intrinsics;
using MLL.ImageLoader;
using System.Text;

namespace MLL;

public class Net
{
    private static void PrepareTraining(IImageDataSetProvider imageProvider)
    {
        var keys = Enumerable.Range(0, 10).ToList();
        var count = imageProvider.GetLargestImageDataSetCount(keys);
        ImageDataSetProviderExtensions.EnsureKeys(count);

        Console.WriteLine("Loading and process images...");
        imageProvider.LoadAllImages(keys);
        Console.WriteLine("Images loaded\n");
    }

    public void Train(IImageDataSetProvider imageProvider, IReadOnlyList<INeuron> neurons)
    {
        var errors = new Dictionary<int, double>(neurons.Count);
        var dt = DateTime.Now;

        void ClearErrors()
        {
            for (int i = 0; i < neurons.Count; i++)
                errors[i] = 0;
        }

        PrepareTraining(imageProvider);
        ClearErrors();

        var message = new StringBuilder(512);


        var options = new ParallelOptions { MaxDegreeOfParallelism = 10 };

        for (int epoch = 0; epoch < 500; epoch++)
        {
            message.AppendLine($"Epoch: {epoch}");
            
            Parallel.For(0, 10, options, neuronNumber =>
            {
                var neuron = neurons[neuronNumber];

                for (int imageNumber = 0; imageNumber < 10; imageNumber++)
                {
                    var imagesSet = imageProvider.GetDataSet(imageNumber);
                    var expected = neuronNumber == imageNumber ? 1 : 0;

                    for (int imageIndex = 0; imageIndex < imagesSet.Count; imageIndex++)
                    {
                        var image = imagesSet[imageIndex];
                        var error = neuron.Train(image.Data, expected);
                        errors[neuronNumber] += Math.Abs(error);
                    }
                }
            });

            var v = Vector256<double>.Zero;
            
            for (var i = 0; i < neurons.Count; i++)
                message.AppendLine($"Neuron: {i}; Error: {errors[i]}");

            var hasError = errors.Values.Sum() != 0;

            ClearErrors();
            Console.WriteLine(message.ToString());
            message.Clear();

            if (!hasError) break;
        }

        Console.WriteLine($"Training ended in {DateTime.Now - dt}\n");
    }

    public double Test2(INeuron[] neurons, IImageDataSet imageSet)
    {
        var error = 0;
        var results = new double[neurons.Length];

        for (int i = 0; i < imageSet.Count; i++)
        {
            var imageData = imageSet[i];

            for (int neuronIndex = 0; neuronIndex < neurons.Length; neuronIndex++)
                results[neuronIndex] = neurons[neuronIndex].Predict(imageData.Data);

            var max = results[0];
            var index = 0;
            
            for (int resultIndex = 1; resultIndex < neurons.Length; resultIndex++)
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

        var successPercents = (1.0 - error / (double) imageSet.Count) * 100;
        var successString = successPercents.ToString("F3").Replace(',', '.');

        Console.WriteLine($"{imageSet.Value}: {successString}");
        return successPercents;
    }

    public double Test(INeuron[] neurons, IImageDataSet imageSet)
    {
        var count = neurons.Length * imageSet.Count;
        var error = 0;

        for (int i = 0; i < imageSet.Count; i++)
        {
            var imageData = imageSet[i];

            for (int neuronIndex = 0; neuronIndex < neurons.Length; neuronIndex++)
            {
                var neuron = neurons[neuronIndex];
                var predict = neuron.Predict(imageData.Data);

                if (neuronIndex.Equals(imageData.Value))
                {
                    if (predict <= 0.975) error++;
                }
                else
                {
                    if (predict > 0.975) error++;
                }
            }
        }

        var successPercents = (1.0 - error / (double) count) * 100;
        var successString = successPercents.ToString("F3").Replace(',', '.');

        Console.WriteLine($"{imageSet.Value}: {successString}");
        return successPercents;
    }

    public void CheckRecognition(INeuron[] neurons)
    {
        var image = ImageTools.LoadImageData(ArgumentParser.GetImagePath(), ImageDataSetOptions.Default);

        for (var i = 0; i < neurons.Length; i++)
        {
            var neuron = neurons[i];
            var predict = neuron.Predict(image);
            Console.WriteLine($"Neuron: {i}; Value: {predict:F3}");
        }
    }
}
