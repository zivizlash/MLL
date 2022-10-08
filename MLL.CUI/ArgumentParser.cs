namespace MLL.CUI;

using Key = ConsoleKey;

public struct ArgumentParser
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

    public static bool IsExitRequested()
    {
        return Console.KeyAvailable && Console.ReadKey(true).Key == Key.Q;
    }

    public static ArgumentParser GetArguments(Key defaultKey = default)
    {
        Key key = defaultKey;
        
        var allowedKeys = new [] { Key.L, Key.T, Key.C, Key.R, Key.I };

        while (!allowedKeys.Contains(key))
        {
            Console.WriteLine("Load - L; Retrain - R; Check - C; Train - T; Image Normalizing - I");
            key = Console.ReadKey(true).Key;
        }
        
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
