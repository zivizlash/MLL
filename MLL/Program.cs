using ImageMagick;

namespace MLL;

public class ImageData
{
    public int Number { get; }
    public double[] Data { get; }

    public ImageData(int number, double[] data)
    {
        Number = number;
        Data = data;
    }
}

public class ImageLoader
{
    public static IEnumerable<ImageData> Load(string path)
    {
        foreach (var file in Directory.EnumerateFiles(path))
        {
            var filename = Path.GetFileNameWithoutExtension(file);

            if (!int.TryParse(filename, out var number) || number is < 0 or > 9) 
                continue;

            using var imageStream = new FileStream(Path.Combine(file), FileMode.Open);
            using var image = new MagickImage(imageStream);

            var pixels = image.GetPixels().ToByteArray(PixelMapping.RGB) 
                ?? throw new InvalidOperationException();
            
            yield return new ImageData(number, RgbToGreyscale(pixels));
        }
    }

    private static double[] RgbToGreyscale(byte[] pixels)
    {
        var greyscale = new double[pixels.Length / 3];

        for (int pi = 0, ri = 0; pi < pixels.Length; pi += 3, ri++)
        {
            var (r, g, b) = (pixels[pi], pixels[pi + 1], pixels[pi + 2]);
            greyscale[ri] = (r + g + b) / (255.0 * 3);
        }

        return greyscale;
    }
}

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
                    var expected = image.Number == neuronIndex ? 1 : 0;

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
            
            if (neuronIndex == imageData.Number)
            {
                Console.WriteLine(Math.Abs(predict - 1) < 0.001
                    ? $"Определение числа {neuronIndex} отработало правильно"
                    : $"Определение числа {neuronIndex} отработало неправильно");
            }
            else
            {
                if (predict != 0)
                    Console.WriteLine($"Число {neuronIndex} отработало неправильно");
            }
        }
    }

    public static void Main()
    {
        var images = ImageLoader.Load("Images")
            .OrderBy(image => image.Number)
            .ToList();

        foreach (var image in images)
            Console.WriteLine($"Image loaded: {image.Number}");

        var neurons = new Neuron[10];
        var random = new Random(150345340);
        
        for (int i = 0; i < neurons.Length; i++)
            neurons[i] = new Neuron(15, 3, 0.1).FillRandomValues(random);

        Train(neurons, images);

        Console.WriteLine("Сетка обучена\n");

        for (int i = 0; i <= 9; i++) Test(neurons, images[i]);
    }
}
