using System.Text;
using MLL.ImageLoader;

namespace MLL;

public class Net
{
    public void Train(IImageDataSetProvider imageProvider, IReadOnlyList<INeuron> neurons)
    {
        var errors = new Dictionary<int, double>(neurons.Count);
        
        void ClearErrors()
        {
            for (int i = 0; i < neurons.Count; i++)
                errors[i] = 0;
        }

        var keys = Enumerable.Range(0, 10).ToList();
        var count = imageProvider.GetLargestImageDataSetCount(keys);
        ImageDataSetProviderExtensions.EnsureKeys(count);
        imageProvider.LoadAllImages(keys);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 10
        };

        ClearErrors();

        var messageBuilder = new StringBuilder();

        for (int epoch = 0; epoch < 700; epoch++)
        {
            messageBuilder.AppendLine($"Epoch: {epoch}");
            
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
            {
                var neuron = errors[i];
                messageBuilder.AppendLine($"Neuron: {i}; Error: {neuron}");
            }
            
            var hasError = errors.Values.Sum() != 0;

            ClearErrors();
            Console.WriteLine(messageBuilder.ToString());
            messageBuilder.Clear();

            if (!hasError) break;
        }

        Console.WriteLine("Train ended\n");
    }

    public void Test(INeuron[] neurons, IImageDataSet imageSet)
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

        Console.WriteLine($"Predicted: {1.0 - error / (double)count}");
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
