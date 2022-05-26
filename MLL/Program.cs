using MLL.ImageLoader;

namespace MLL;

public class Program
{
    private static INeuron[] GetNeurons(bool loadFromDisk, ImageRecognitionOptions options)
    {
        // ReSharper disable once CoVariantArrayConversion
        if (loadFromDisk)
            return NeuronWeightsSaver.Load<LinearNeuron>();
        
        var neurons = new INeuron[10];

        var random = options.RandomSeed.HasValue
            ? new Random(options.RandomSeed.Value)
            : new Random();

        var weightsCount = options.ImageWidth * options.ImageHeight;

        for (int i = 0; i < neurons.Length; i++)
        {
            neurons[i] = new LinearNeuron(weightsCount, options.Bias, options.LearningRate);
            neurons[i].FillRandomValues(random);
        }
        
        return neurons;
    }
    
    private static IImageDataSetProvider CreateDataSetProvider(bool isEven) =>
        new FolderNameDataSetProvider(new NotOrEvenFilesProviderFactory(isEven), 
            NameToFolder, ImageDataSetOptions.Default);

    private static IImageDataSetProvider CreateTestDataSetProvider() => CreateDataSetProvider(false);
    private static IImageDataSetProvider CreateTrainDataSetProvider() => CreateDataSetProvider(true);

    public static void Main()
    {
        var args = ArgumentParser.GetArguments();
        
        var neurons = GetNeurons(args.LoadFromDisk, ImageRecognitionOptions.Default);
        var net = new Net();

        if (args.Train)
            net.Train(CreateTrainDataSetProvider(), neurons);

        var imageSetTestProvider = CreateTestDataSetProvider();

        if (!args.CheckRecognition)
            for (int i = 0; i <= 9; i++) 
                net.Test(neurons, imageSetTestProvider.GetDataSet(i));

        if (args.CheckRecognition)
            net.CheckRecognition(neurons);

        if (args.Train)
            NeuronWeightsSaver.Save(neurons);
    }

    private static string NameToFolder(string name) =>
        $"C:\\Auto\\datasets\\Font\\Font\\Sample{int.Parse(name) + 1:d3}";
}
