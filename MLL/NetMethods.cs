using MLL.ImageLoader;

namespace MLL;

public class ErrorCalculator
{

}

public class NeuronLayer
{
    private readonly SigmoidNeuron[] _neurons;

    public ReadOnlySpan<SigmoidNeuron> Neurons => _neurons;
    public int Count => _neurons.Length;

    public NeuronLayer(int neuronsCount, int weightsCount, double learningRate, bool useActivationFunc)
    {
        _neurons = new SigmoidNeuron[neuronsCount];

        for (int i = 0; i < _neurons.Length; i++)
            _neurons[i] = new SigmoidNeuron(weightsCount, learningRate, useActivationFunc);
    }

    // Метод берет данные и прогоняет по всем нейронам.
    public double[] Predict(double[] input)
    {
        var neuronOutputs = new double[_neurons.Length];

        for (var i = 0; i < _neurons.Length; i++)
        {
            var neuron = _neurons[i];
            neuronOutputs[i] = neuron.Predict(input);
        }

        return neuronOutputs;
    }

    //public double[] Train(double[] input, double[] expected, NeuronLayer? previous = null)
    //{
    //    var errors = new double[_neurons.Length];

    //    // Ошибка каждого конкретного нейрона распространяется согласно
    //    // весам на входах этого нейрона от предыдущего нейрона.
        
    //    for (int i = 0; i < _neurons.Length; i++)
    //    {
    //        var neuron = _neurons[i];
    //        var expectedResult = expected[i];

    //        var error = neuron.Train(input, expectedResult);
    //        errors[i] = error;

    //        var weightsSum = neuron.CalculateWeightsSum();
    //        var previousError = error * neuron.Weights[i] / weightsSum;
    //    }

    //    return errors;
    //}
}

public class NetMethods
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

    public void Train(IImageDataSetProvider imageProvider, Net neurons)
    {
        var dt = DateTime.Now;
        PrepareTraining(imageProvider);
        
        for (int epoch = 0; epoch < 500; epoch++)
        {
            double errorAcc = 0;

            for (int imageNumber = 0; imageNumber < 10; imageNumber++)
            {
                var imagesSet = imageProvider.GetDataSet(imageNumber);
                
                for (int imageIndex = 0; imageIndex < imagesSet.Count; imageIndex++)
                {
                    var image = imagesSet[imageIndex];

                    var expected = new double[neurons.OutputNeuronCount];
                    expected[imageNumber] = 1;

                    var errors = neurons.Train(image.Data, expected);
                    foreach (double error in errors) errorAcc += error;
                }
            }
            
            Console.WriteLine($"Net Error: {errorAcc}");
        }

        Console.WriteLine($"Training ended in {DateTime.Now - dt}\n");
    }

    public double Test2(Neuron[] neurons, IImageDataSet imageSet)
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

    public double Test(Neuron[] neurons, IImageDataSet imageSet)
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

    public void CheckRecognition(Neuron[] neurons)
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
