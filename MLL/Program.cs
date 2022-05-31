using System.Numerics;
using System.Runtime.Intrinsics.X86;
using ImageMagick;
using MLL.ImageLoader;

namespace MLL;

public class Program
{
    private static INeuron[] GetNeurons(bool loadFromDisk, ImageRecognitionOptions options)
    {
        // ReSharper disable once CoVariantArrayConversion
        if (loadFromDisk)
            return NeuronWeightsSaver.Load<SigmoidNeuron>();
        
        var neurons = new INeuron[10];

        var random = options.RandomSeed.HasValue
            ? new Random(options.RandomSeed.Value)
            : new Random();

        var weightsCount = options.ImageWidth * options.ImageHeight;

        for (int i = 0; i < neurons.Length; i++)
        {
            var neuron = new SigmoidNeuron(weightsCount, options.LearningRate);
            neurons[i] = neuron; // .FillRandomValues(random, 0.5);
        }
        
        return neurons;
    }
    
    private static IImageDataSetProvider CreateDataSetProvider(bool isEven) =>
        new FolderNameDataSetProvider(new NotOrEvenFilesProviderFactory(isEven), 
            NameToFolder, ImageDataSetOptions.Default);

    private static IImageDataSetProvider CreateTestDataSetProvider() => CreateDataSetProvider(false);
    private static IImageDataSetProvider CreateTrainDataSetProvider() => CreateDataSetProvider(true);

    private static void VectorTest()
    {
        var vectorSize = Vector<double>.Count;
        var accVector = Vector<double>.Zero;

        double[] values = { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

        int i;

        for (i = 0; i < values.Length - vectorSize; i += vectorSize)
        {
            var v = new Vector<double>(values, i);
            accVector = Vector.Add(accVector, v);
        }
        
        double result = Vector.Dot(accVector, Vector<double>.One);

        for (; i < values.Length; i++)
            result += values[i];

        Console.WriteLine($"IsHardwareAccelerated: {Vector.IsHardwareAccelerated}");
        Console.WriteLine($"VectorSize<double>: {vectorSize}");
        Console.WriteLine($"ZeroVector: {accVector}");
        Console.WriteLine($"Result: {result}");
    }

    public static void Main()
    {
        VectorTest();

        return;
        var args = ArgumentParser.GetArguments();
        
        var neurons = GetNeurons(args.LoadFromDisk, ImageRecognitionOptions.Default);
        var net = new Net();

        if (args.Train)
            net.Train(CreateTrainDataSetProvider(), neurons);
        
        if (!args.CheckRecognition && !args.TestImageNormalizing)
        {
            var imageSetTestProvider = CreateTestDataSetProvider();

            double recognizedPercents = 0;
            
            for (int i = 0; i < 10; i++)
                recognizedPercents += net.Test2(neurons, imageSetTestProvider.GetDataSet(i));

            Console.WriteLine();
            Console.WriteLine($"Overall recognized percents: {recognizedPercents / 10.0}");
        }

        if (args.CheckRecognition)
            net.CheckRecognition(neurons);

        if (args.Train)
            NeuronWeightsSaver.Save(neurons);

        if (args.TestImageNormalizing)
        {
            var imagePath = ArgumentParser.GetImagePath();
            var imageData = ImageTools.LoadImageData(imagePath, ImageDataSetOptions.Default);

            byte[] imageBytes = new byte[imageData.Length * 3];

            for (int di = 0, bi = 0; di < imageData.Length; di++, bi += 3)
            {
                byte byteValue = (byte) (imageData[di] * 255);
                imageBytes[bi + 0] = byteValue;
                imageBytes[bi + 1] = byteValue;
                imageBytes[bi + 2] = byteValue;
            }
            
            var settings = new PixelReadSettings(128, 128, StorageType.Char, PixelMapping.RGB);
            using var image = new MagickImage(imageBytes, settings);

            image.Format = MagickFormat.Png;
            image.Write("test.png");
        }
    }

    private static string NameToFolder(string name) =>
        $"C:\\Auto\\datasets\\Font\\Font\\Sample{int.Parse(name) + 1:d3}";
}
