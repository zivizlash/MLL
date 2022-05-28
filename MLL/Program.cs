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
            var neuron = new LinearNeuron(weightsCount, options.Bias, options.LearningRate);
            neurons[i] = neuron.FillRandomValues(random);
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

        if (!args.CheckRecognition)
        {
            var imageSetTestProvider = CreateTestDataSetProvider();

            double recognizedPercents = 0;

            for (int i = 0; i < 10; i++)
                recognizedPercents += net.Test(neurons, imageSetTestProvider.GetDataSet(i));

            Console.WriteLine();
            Console.WriteLine($"Overall recognized percents: {recognizedPercents / 10.0}");
        }

        if (args.CheckRecognition)
            net.CheckRecognition(neurons);

        if (args.Train)
            NeuronWeightsSaver.Save(neurons);
    }

    private static string NameToFolder(string name) =>
        $"C:\\Auto\\datasets\\Font\\Font\\Sample{int.Parse(name) + 1:d3}";
}
