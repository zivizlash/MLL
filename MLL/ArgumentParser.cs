namespace MLL;

using Key = ConsoleKey;

public class ArgumentParser
{
    public bool LoadFromDisk { get; }
    public bool CheckRecognition { get; }
    public bool Train { get; }
    public bool TestImageNormalizing { get; }

    public ArgumentParser(bool loadFromDisk, bool checkRecognition, bool train, bool testImageNormalizing)
    {
        LoadFromDisk = loadFromDisk;
        CheckRecognition = checkRecognition;
        Train = train;
        TestImageNormalizing = testImageNormalizing;
    }

    public static ArgumentParser GetArguments()
    {
        Key key;

        var allowedKeys = new [] { Key.L, Key.T, Key.C, Key.R, Key.I };

        do
        {
            Console.WriteLine("Load - L; Retrain - R; Check - C; Train - T; Image Normalizing - I");
            key = Console.ReadKey(true).Key;
        }
        while (!allowedKeys.Contains(key));

        Console.WriteLine(key switch
        {
            Key.L => "Loading weights from disk",
            Key.T => "Training",
            Key.C => "Check recognition",
            Key.R => "Retrain",
            Key.I => "Image normalizing test",
            _ => throw new InvalidOperationException()
        });

        return new ArgumentParser(
            loadFromDisk: key is Key.L or Key.C or Key.T,
            checkRecognition: key is Key.C,
            train: key is Key.R or Key.T,
            testImageNormalizing: key is Key.I);
    }

    public static string GetImagePath()
    {
        Console.WriteLine("Enter image path: ");
        return Console.ReadLine() ?? throw new ArgumentNullException();
    }
}
