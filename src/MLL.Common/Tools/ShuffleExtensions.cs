namespace MLL.Common.Tools;

public static class ShuffleExtensions
{
    public static void ShuffleInPlace<T>(this T[] items, Random? random = null)
    {
        var rnd = random ?? new Random();

        int sndLength = items.Length / 2;
        var fstLength = items.Length - sndLength;

        for (int i = sndLength; i < items.Length; i++)
        {
            var swapIndex = rnd.Next(fstLength + 1);
            (items[swapIndex], items[i]) = (items[i], items[swapIndex]);
        }
    }
}
