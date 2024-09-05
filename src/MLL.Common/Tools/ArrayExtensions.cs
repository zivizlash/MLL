namespace MLL.Common.Tools;

public static class ArrayExtensions
{
    private static void Throw(string name)
    {
        throw new ArgumentOutOfRangeException(name);
    }

    public static (int, TOut) FindMax<TIn, TOut>(this TIn[] source, Func<TIn, TOut> selector, IComparer<TOut>? valueComparer = null)
    {
        Check.NotNull(source, nameof(source));
        Check.NotNull(selector, nameof(selector));

        if (source.Length < 1) Throw(nameof(source));

        var comparer = valueComparer ?? Comparer<TOut>.Default;

        int currentIndex = 0;
        TOut currentValue = selector.Invoke(source[0]);

        for (int i = 1; i < source.Length; i++)
        {
            TOut value = selector.Invoke(source[i]);

            if (comparer.Compare(value, currentValue) >= 0)
            {
                currentValue = value;
                currentIndex = i;
            }
        }

        return (currentIndex, currentValue);
    }
}
