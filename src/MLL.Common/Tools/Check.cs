namespace MLL.Common.Tools;

public class Throw
{
    public static void ArgumentOutOfRange(string name) => throw new ArgumentOutOfRangeException(name);
}

public class Check
{
    public static void LengthEqual(int len1, int len2, string name)
    {
        if (len1 != len2) Throw.ArgumentOutOfRange(name);
    }
}
