namespace MLL;

public class ArgumentParser
{
    public bool LoadFromDisk { get; }
    public bool CheckRecognition { get; }
    public bool Train => !LoadFromDisk;
    
    public ArgumentParser(bool loadFromDisk, bool checkRecognition)
    {
        LoadFromDisk = loadFromDisk;
        CheckRecognition = checkRecognition;
    }

    public static ArgumentParser GetArguments()
    {
        ConsoleKey key;

        do
        {
            Console.WriteLine("Load - L; Train - T; Check - C");
            key = Console.ReadKey(true).Key;
        }
        while (key is not (ConsoleKey.L or ConsoleKey.T or ConsoleKey.C));

        Console.WriteLine(key switch
        {
            ConsoleKey.L => "Loading weights from disk",
            ConsoleKey.T => "Training",
            ConsoleKey.C => "Check recognition",
            _ => throw new InvalidOperationException()
        });

        return new ArgumentParser(
            loadFromDisk: key is ConsoleKey.L or ConsoleKey.C,
            checkRecognition: key is ConsoleKey.C);
    }

    public static string GetImagePath()
    {
        Console.WriteLine("Enter image path: ");
        return Console.ReadLine() ?? throw new ArgumentNullException();
    }
}
