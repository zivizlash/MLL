using Newtonsoft.Json;

namespace MLL;

public static class NeuronWeightsSaver
{
    public static void Save(Neuron[] neurons)
    {
        var json = JsonConvert.SerializeObject(neurons);
        File.WriteAllText("neurons.json", json);
    }

    public static Neuron[] Load()
    {
        var json = File.ReadAllText("neurons.json");
        return JsonConvert.DeserializeObject<Neuron[]>(json)!;
    }
}

public class Program
{
    private static void Train(IImageDataSetProvider imageProvider, IReadOnlyList<Neuron> neurons)
    {
        var errors = new Dictionary<int, double>(neurons.Count);

        for (int i = 0; i < neurons.Count; i++) errors[i] = 0;

        void ClearErrors()
        {
            foreach (var key in errors!.Keys) errors[key] = 0;
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
    }

    private static void Test(Neuron[] neurons, IImageDataSet imageSet)
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
                    if (Math.Abs(predict - 1) < 0.001)
                    {
                        //Console.WriteLine($"Определение числа {neuronIndex} отработало правильно");
                    }
                    else
                    {
                        //Console.WriteLine($"Определение числа {neuronIndex} отработало неправильно");
                        error++;
                    }
                }
                else
                {
                    var isNotRight = predict != 0;

                    if (isNotRight)
                    {
                        //Console.WriteLine($"Число {neuronIndex} отработало неправильно");
                        error++;
                    }
                }
            }
        }

        Console.WriteLine($"Predicted: {1.0 - error / (double)count}");
    }

    private static string NameToFolder(string name) => 
        $"C:\\Auto\\datasets\\Font\\Font\\Sample{int.Parse(name) + 1:d3}";

    public static void Main()
    {
        ConsoleKeyInfo key;

        do
        {
            Console.WriteLine("Load - R; Learn - T; Test - U");
            key = Console.ReadKey(true);
        } 
        while (key.Key is not (ConsoleKey.R or ConsoleKey.T or ConsoleKey.U));

        var test = key.Key == ConsoleKey.U;
        var load = test || key.Key == ConsoleKey.R;

        var imageProvider = new FolderNameDataSetProvider(
            NameToFolder, new ImageDataSetOptions(128, 128));

        Neuron[] neurons;

        if (load)
        {
            neurons = NeuronWeightsSaver.Load();
        }
        else
        {
            neurons = new Neuron[10];
            var random = new Random();

            for (int i = 0; i < neurons.Length; i++)
                neurons[i] = new Neuron(128 * 128, 10, 0.01).FillRandomValues(random);

            Train(imageProvider, neurons);

            Console.WriteLine("Сетка обучена\n");
        }

        for (int i = 0; i <= 9; i++) 
            Test(neurons, imageProvider.GetDataSet(i));

        if (test)
        {
            Console.WriteLine("Enter image path: ");
            var path = Console.ReadLine() ?? throw new ArgumentNullException();

            var image = ImageTools.LoadImageDataRaw(path, ImageDataSetOptions.Default);

            for (var i = 0; i < neurons.Length; i++)
            {
                var neuron = neurons[i];
                Console.WriteLine($"Neuron: {i}; Value: {neuron.Predict(image)}");
            }
        }

        if (!load) NeuronWeightsSaver.Save(neurons);
    }
}
