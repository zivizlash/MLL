using ImageMagick;
using MLL.ImageLoader;

namespace MLL;

public class Program
{
    private static Random GetRandomBySeed(int? seed) =>
        seed.HasValue ? new Random(seed.Value) : new Random();

    private static Net GetNeurons(bool loadFromDisk, ImageRecognitionOptions options, bool fillRandom = false)
    {
        // ReSharper disable once CoVariantArrayConversion
        if (loadFromDisk)
        {
            throw new NotImplementedException();
            //return NeuronWeightsSaver.Load<SigmoidNeuron>();
        }

        return CreateWithHiddenLayers();

        //var neurons = new Neuron[10];
        //var random = GetRandomBySeed(options.RandomSeed);

        //var weightsCount = options.ImageWidth * options.ImageHeight;

        //for (int i = 0; i < neurons.Length; i++)
        //{
        //    var neuron = new SigmoidNeuron(weightsCount, options.LearningRate);
        //    neurons[i] = fillRandom ? neuron.FillRandomValues(random) : neuron;
        //}

        ////var net = new Net()
        
        //return neurons;
    }
    
    private static IImageDataSetProvider CreateDataSetProvider(bool isEven) =>
        new FolderNameDataSetProvider(new NotOrEvenFilesProviderFactory(isEven), 
            NameToFolder, ImageDataSetOptions.Default);

    private static IImageDataSetProvider CreateTestDataSetProvider() => CreateDataSetProvider(false);
    private static IImageDataSetProvider CreateTrainDataSetProvider() => CreateDataSetProvider(true);

    private const int NumbersCount = 10;

    private static Net CreateWithHiddenLayers()
    {
        static LayerDefinition CreateLayer(int count, int weights) => LayerDefinition.CreateSingle(count, weights);

        var imageOptions = ImageRecognitionOptions.Default;
        var imageWeightsCount = imageOptions.ImageWidth * imageOptions.ImageHeight;

        throw new NotImplementedException();

        //return new Net(CreateLayer(10, imageWeightsCount), CreateLayer(NumbersCount, 10));
    }

    private static void Check(Net net, double[][] input, double[] results)
    {
        for (int i = 0; i < results.Length; i++)
        {
            var result = net.Predict(input[i])[0];
            var expected = results[i];

            Console.WriteLine($"{string.Join(' ', input[i])}: {expected}; actual: {result}");
        }
    }

    public static void Main()
    {
        //var args = ArgumentParser.GetArguments();

        //var net = GetNeurons(args.LoadFromDisk, ImageRecognitionOptions.Default);
        
        var input = new[]
        {
            new double[] { 0, 0 }, 
            new double[] { 1, 1 },
            new double[] { 1, 0 }, 
            new double[] { 0, 1 }
        };

        var results = new double[] 
        {
            0, 0, 1, 1
        };
        
        var net = new Net(0.1, 
            LayerDefinition.CreateSingle(4, 2),
            LayerDefinition.CreateSingle(4, 4),
            LayerDefinition.CreateSingle(1, 4, false));

        var random = new Random(600);

        net.FillRandomValues(random, 2);

        for (int epoch = 0; epoch < 6000; epoch++)
        {
            double generalError = 0;

            Check(net, input, results);
            
            for (int resultIndex = 0; resultIndex < results.Length; resultIndex++)
            {
                var expectedNumber = results[resultIndex];
                var expected = new[] { expectedNumber };
                var output = net.Train(input[resultIndex], expected)[0];

                var error = output - expectedNumber;
                generalError += Math.Abs(error);
            }
            
            Console.WriteLine($"General Error: {generalError:F5}\n");
        }

        for (int i = 0; i < results.Length; i++)
        {
            var result = net.Predict(input[i])[0];
            var expected = results[i];

            Console.WriteLine($"{string.Join(' ', input[i])}: {expected}; actual: {result}");
        }

        //var netMethods = new NetMethods();

        //if (args.Train)
        //    netMethods.Train(CreateTrainDataSetProvider(), net);

        //if (!args.CheckRecognition && !args.TestImageNormalizing)
        //{
        //    var imageSetTestProvider = CreateTestDataSetProvider();

        //    double recognizedPercents = 0;

        //    for (int i = 0; i < 10; i++)
        //        recognizedPercents += netMethods.Test2(net, imageSetTestProvider.GetDataSet(i));

        //    Console.WriteLine();
        //    Console.WriteLine($"Overall recognized percents: {recognizedPercents / 10.0}");
        //}

        //if (args.CheckRecognition)
        //    netMethods.CheckRecognition(net);

        //if (args.Train)
        //    NeuronWeightsSaver.Save(net);

        //if (args.TestImageNormalizing)
        //    TestNormalizing();
    }

    private static void TestNormalizing()
    {
        var options = ImageDataSetOptions.Default;
        var imagePath = ArgumentParser.GetImagePath();
        var imageData = ImageTools.LoadImageData(imagePath, options);

        byte[] imageBytes = new byte[imageData.Length * 3];

        for (int di = 0, bi = 0; di < imageData.Length; di++, bi += 3)
        {
            byte byteValue = (byte) (imageData[di] * 255);
            imageBytes[bi + 0] = byteValue;
            imageBytes[bi + 1] = byteValue;
            imageBytes[bi + 2] = byteValue;
        }

        var settings = new PixelReadSettings(options.Width, options.Height, StorageType.Char, PixelMapping.RGB);
        using var image = new MagickImage(imageBytes, settings);

        image.Format = MagickFormat.Png;
        image.Write("test.png");
    }

    private static string NameToFolder(string name) =>
        $"C:\\Auto\\datasets\\Font\\Font\\Sample{int.Parse(name) + 1:d3}";
}
