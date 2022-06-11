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
            return NeuronWeightsSaver.Load();
        }

        return CreateWithHiddenLayers(options);
    }
    
    private static IImageDataSetProvider CreateDataSetProvider(bool isEven) =>
        new FolderNameDataSetProvider(new NotOrEvenFilesProviderFactory(isEven), 
            NameToFolder, ImageDataSetOptions.Default);

    private static IImageDataSetProvider CreateTestDataSetProvider() => CreateDataSetProvider(false);
    private static IImageDataSetProvider CreateTrainDataSetProvider() => CreateDataSetProvider(true);

    private static Net CreateWithHiddenLayers(ImageRecognitionOptions options)
    {
        static LayerDefinition CreateLayer(int count, int weights, bool useActivation = true) => 
            LayerDefinition.CreateSingle(count, weights, useActivation);
        
        const int numbersCount = 10;

        var imageWeightsCount = options.ImageWidth * options.ImageHeight;

        var layers = new[]
        {
            CreateLayer(numbersCount, imageWeightsCount),
            CreateLayer(numbersCount * 2, numbersCount),
            CreateLayer(numbersCount, numbersCount * 2, false)
        };

        return new Net(options.LearningRate, layers).FillRandomValues(GetRandomBySeed(options.RandomSeed), 0.1);
    }
    
    public static void Main()
    {
        var args = ArgumentParser.GetArguments();

        var imageOptions = ImageRecognitionOptions.Default;

        var net = GetNeurons(args.LoadFromDisk, imageOptions);
        
        net.UpdateLearningRate(imageOptions.LearningRate);

        var netMethods = new NetMethods(net);

        if (args.Train)
            netMethods.Train(CreateTrainDataSetProvider());

        if (!args.CheckRecognition && !args.TestImageNormalizing)
            netMethods.FullTest(CreateTestDataSetProvider());

        if (args.CheckRecognition)
            throw new NotImplementedException(); // netMethods.CheckRecognition();

        if (args.Train)
            NeuronWeightsSaver.Save(net);

        if (args.TestImageNormalizing)
            TestNormalizing();
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
