using MLL.ImageLoader;

namespace MLL;

public class Net
{
    public void Train(IImageDataSetProvider imageProvider, IReadOnlyList<INeuron> neurons)
    {
        var errors = new Dictionary<int, double>(neurons.Count);

        for (int i = 0; i < neurons.Count; i++) errors[i] = 0;

        void ClearErrors()
        {
            foreach (var key in errors.Keys) errors[key] = 0;
        }

        for (int epoch = 0; epoch < 200; epoch++)
        {
            Console.WriteLine($"Epoch: {epoch}");

            bool hasError = false;

            for (int number = 0; number < 10; number++)
            {
                var neuron = neurons[number];

                for (int imageNumber = 0; imageNumber < 10; imageNumber++)
                {
                    var imagesSet = imageProvider.GetDataSet(imageNumber);
                    var expected = number == imageNumber ? 1 : 0;

                    for (int imageIndex = 0; imageIndex < 20; imageIndex++)
                    {
                        var image = imagesSet[imageIndex];
                        var error = neuron.Train(image.Data, expected);

                        errors[number] += Math.Abs(error);
                        hasError |= error != 0;
                    }
                }
            }

            for (var i = 0; i < neurons.Count; i++)
            {
                var neuron = errors[i];
                Console.WriteLine($"Neuron: {i}; Whole error: {neuron}");
            }

            ClearErrors();
            Console.WriteLine();
            if (!hasError) break;
        }

        Console.WriteLine("Train ended\n");
    }

    public void Test(INeuron[] neurons, IImageDataSet imageSet)
    {
        var count = 0;
        var error = 0;

        for (int i = 0; i < 20; i++)
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
