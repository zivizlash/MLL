namespace MLL;

public class ArgumentParser
{
    public bool LoadFromDisk { get; }
    public bool CheckRecognition { get; }
    public bool Train { get; }

    public ArgumentParser(bool loadFromDisk, bool checkRecognition, bool train)
    {
        LoadFromDisk = loadFromDisk;
        CheckRecognition = checkRecognition;
        Train = train;
    }

    public static ArgumentParser GetArguments()
    {
        ConsoleKey key;

        do
        {
            Console.WriteLine("Load - L; Retrain - R; Check - C; Train - T");
            key = Console.ReadKey(true).Key;
        }
        while (key is not (ConsoleKey.L or ConsoleKey.T or ConsoleKey.C or ConsoleKey.R));

        Console.WriteLine(key switch
        {
            ConsoleKey.L => "Loading weights from disk",
            ConsoleKey.T => "Training",
            ConsoleKey.C => "Check recognition",
            ConsoleKey.R => "Retrain",
            _ => throw new InvalidOperationException()
        });

        return new ArgumentParser(
            loadFromDisk: key is ConsoleKey.L or ConsoleKey.C or ConsoleKey.T,
            checkRecognition: key is ConsoleKey.C,
            train: key is ConsoleKey.R or ConsoleKey.T);
    }

    public static string GetImagePath()
    {
        Console.WriteLine("Enter image path: ");
        return Console.ReadLine() ?? throw new ArgumentNullException();
    }
}
