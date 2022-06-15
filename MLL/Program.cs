using ImageMagick;
using MLL.Builders;
using MLL.ImageLoader;
using MLL.Neurons;
using MLL.Options;
using MLL.Saving;
using MLL.Statistics;
using MLL.Tools;

namespace MLL;

public class Program
{
    private static Random GetRandomBySeed(int? seed) =>
        seed.HasValue ? new Random(seed.Value) : new Random();
    
    private static Net GetNeurons(bool loadFromDisk, ImageRecognitionOptions options, LayerDefinition[] layers)
    {
        var net = loadFromDisk
            ? NeuronWeightsSaver.Load()
            : CreateWithHiddenLayers(options, layers);

        return net.UpdateLearningRate(options.LearningRate);
    }
    
    private static IImageDataSetProvider CreateDataSetProvider(bool isEven) =>
        new FolderNameDataSetProvider(new NotOrEvenFilesProviderFactory(isEven), 
            NameToFolder, ImageDataSetOptions.Default);

    private static IImageDataSetProvider CreateTestDataSetProvider() => CreateDataSetProvider(false);
    private static IImageDataSetProvider CreateTrainDataSetProvider() => CreateDataSetProvider(true);

    private static Net CreateWithHiddenLayers(ImageRecognitionOptions options, LayerDefinition[] layers) =>
        new Net(options.LearningRate, layers).FillRandomValues(GetRandomBySeed(options.RandomSeed), 0.1);

    private static LayerDefinition[] GetLayersDefinition(ImageRecognitionOptions options)
    {
        const int numbersCount = 10;
        var imageWeightsCount = options.ImageWidth * options.ImageHeight;

        return LayerDefinition.Builder
            .WithLearningRate(options.LearningRate)
            .WithInput(numbersCount, imageWeightsCount)
            .WithHiddenLayers(numbersCount * 2)
            .WithOutput(numbersCount, false)
            .Build();
    }
    
    public static void Main()
    {
        var args = ArgumentParser.GetArguments();
        var imageOptions = ImageRecognitionOptions.Default;

        var layers = GetLayersDefinition(imageOptions);
        var net = GetNeurons(args.LoadFromDisk, imageOptions, layers);
        
        var netMethods = new NetMethods(net);

        if (args.Train)
        {
            var trainDataSet = CreateTrainDataSetProvider();
            var testDataSet = CreateTestDataSetProvider();

            var stats = new StatisticsManager(imageOptions, layers, testDataSet, trainDataSet);
            netMethods.Train(trainDataSet, stats);
        }

        if (!args.CheckRecognition && !args.TestImageNormalizing)
            netMethods.FullTest(CreateTestDataSetProvider());

        if (args.CheckRecognition)
            throw new NotImplementedException(); // netMethods.CheckRecognition();

        if (args.Train)
            NeuronWeightsSaver.Save(net);

        if (args.TestImageNormalizing)
            TestImageNormalizing();
    }

    private static void TestImageNormalizing()
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
