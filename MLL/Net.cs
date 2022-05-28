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
        
        void ClearErrors()
        {
            for (int i = 0; i < neurons.Count; i++)
                errors[i] = 0;
        }

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 10 };

        PrepareTraining(imageProvider);
        ClearErrors();

        var message = new StringBuilder(512);

        for (int epoch = 0; epoch < 700; epoch++)
        {
            message.AppendLine($"Epoch: {epoch}");
            
            Parallel.For(0, 10, parallelOptions, number =>
            {
                var neuron = neurons[number];

                for (int imageNumber = 0; imageNumber < 10; imageNumber++)
                {
                    var imagesSet = imageProvider.GetDataSet(imageNumber);
                    var expected = number == imageNumber ? 1 : 0;

                    for (int imageIndex = 0; imageIndex < imagesSet.Count; imageIndex++)
                    {
                        var image = imagesSet[imageIndex];
                        var error = neuron.Train(image.Data, expected);
                        errors[number] += Math.Abs(error);
                    }
                }
            });
            
            for (var i = 0; i < neurons.Count; i++)
                message.AppendLine($"Neuron: {i}; Error: {errors[i]}");

            var hasError = errors.Values.Sum() != 0;

            ClearErrors();
            Console.WriteLine(message.ToString());
            message.Clear();

            if (!hasError) break;
        }

        Console.WriteLine("Training ended\n");
    }

    public double Test(INeuron[] neurons, IImageDataSet imageSet)
    {
        var count = 0;
        var error = 0;

        for (int i = 0; i < imageSet.Count; i++)
        {
            var imageData = imageSet[i];

            for (int neuronIndex = 0; neuronIndex < neurons.Length; neuronIndex++)
            {
                count++;

                var neuron = neurons[neuronIndex];
                var predict = neuron.Predict(imageData.Data);

                if (neuronIndex.Equals(imageData.Value))
                {
                    if (Math.Abs(predict - 1) > 0.001) error++;
                }
                else
                {
                    if (predict != 0) error++;
                }
            }
        }

        var errorPercents = (1.0 - error / (double) count) * 100;
        var errorString = errorPercents.ToString("F3").Replace(',', '.');

        Console.WriteLine($"{imageSet.Value}: {errorString}");
        return errorPercents;
    }

    public void CheckRecognition(INeuron[] neurons)
    {
        var image = ImageTools.LoadImageData(ArgumentParser.GetImagePath(), ImageDataSetOptions.Default);

        for (var i = 0; i < neurons.Length; i++)
        {
            var neuron = neurons[i];
            Console.WriteLine($"Neuron: {i}; Value: {neuron.Predict(image)}");
        }
    }
}
