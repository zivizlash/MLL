namespace MLL.CUI;

using Key = ConsoleKey;

public struct ArgumentParser
{
    public bool LoadFromDisk { get; }
    public bool CheckRecognition { get; }
    public bool Train { get; }

    private static bool _isExitRequested;

    static ArgumentParser()
    {
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            _isExitRequested = true;
        };
    }

    public ArgumentParser(bool loadFromDisk, bool checkRecognition, bool train)
    {
        LoadFromDisk = loadFromDisk;
        CheckRecognition = checkRecognition;
        Train = train;
    }

    public static bool IsExitRequested()
    {
        return Console.KeyAvailable && Console.ReadKey(true).Key == Key.Q || _isExitRequested;
    }

    public static ArgumentParser GetArguments(Key defaultKey = default)
    {
        Key key = defaultKey;
        
        var allowedKeys = new [] { Key.L, Key.T, Key.C, Key.R, Key.I };

        while (!allowedKeys.Contains(key))
        {
            Console.WriteLine("Load - L; Retrain - R; Check - C; Train - T");
            key = Console.ReadKey(true).Key;
        }
        
        Console.WriteLine(key switch
        {
            Key.L => "Loading weights from disk",
            Key.T => "Training",
            Key.C => "Recognition test",
            Key.R => "Retrain",
            _ => throw new InvalidOperationException()
        });

        return new ArgumentParser(
            loadFromDisk: key is Key.L or Key.C or Key.T,
            checkRecognition: key is Key.C,
            train: key is Key.R or Key.T);
    }

    public static string GetImagePath()
    {
        Console.WriteLine("Enter image path: ");
        return Console.ReadLine() ?? throw new ArgumentNullException();
    }
}
