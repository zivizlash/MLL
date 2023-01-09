namespace MLL.Common.Tools;

public static class RandomExtensions
{
    public static float Range(this Random random, float range)
    {
        if (range <= 0.0f)
        {
            Throw.ArgumentOutOfRange(nameof(range));
        }

        var value = (float)random.NextDouble();
        return (value - 0.5f) * 2 * range;
    }
}
