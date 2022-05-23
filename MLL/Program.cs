namespace MLL;

public class Program
{
    private static void Train(IReadOnlyList<Neuron> neurons, IReadOnlyList<ImageData> images)
    {
        for (int epoch = 0; epoch < 100; epoch++)
        {
            Console.WriteLine($"Epoch: {epoch}");

            bool hasError = false;

            foreach (var image in images)
            {
                for (int neuronIndex = 0; neuronIndex < neurons.Count; neuronIndex++)
                {
                    var neuron = neurons[neuronIndex];
                    var expected = neuronIndex.Equals(image.Value) ? 1 : 0;

                    var error = neuron.Train(image.Data, expected);
                    var errorQ = error != 0 ? neuron.LastError : error;

                    hasError |= error != 0;
                    Console.Write($"{errorQ} ");
                }

                Console.WriteLine();
            }

            Console.WriteLine();
            if (!hasError) break;
        }
    }   

    private static void Test(Neuron[] neurons, ImageData imageData)
    {
        for (int neuronIndex = 0; neuronIndex < neurons.Length; neuronIndex++)
        {
            var neuron = neurons[neuronIndex];
            var predict = neuron.Predict(imageData.Data);
            
            if (neuronIndex.Equals(imageData.Value))
            {
                Console.WriteLine(Math.Abs(predict - 1) < 0.001
                    ? $"Определение числа {neuronIndex} отработало правильно"
                    : $"Определение числа {neuronIndex} отработало неправильно");
            }
            else
            {
                if (predict != 0) Console.WriteLine($"Число {neuronIndex} отработало неправильно");
            }
        }
    }

    public static void Main()
    {
        var images = ImageLoader.Load("Images")
            .OrderBy(image => (int)image.Value)
            .ToList();

        foreach (var image in images)
            Console.WriteLine($"Image loaded: {image.Value}");

        var neurons = new Neuron[10];
        var random = new Random(150345340);
        
        for (int i = 0; i < neurons.Length; i++)
            neurons[i] = new Neuron(15, 3, 0.1).FillRandomValues(random);

        Train(neurons, images);

        Console.WriteLine("Сетка обучена\n");

        for (int i = 0; i <= 9; i++) Test(neurons, images[i]);
    }
}
