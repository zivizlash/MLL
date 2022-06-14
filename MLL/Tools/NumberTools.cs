using System.Runtime.CompilerServices;

namespace MLL.Tools;

public static class NumberTools
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Sigmoid(double value)
    {
        return 1.0 / (1.0 + Math.Exp(-value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Sigmoid(float value)
    {
        return 1.0f / (1.0f + MathF.Exp(-value));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float AtomicAdd(ref float location, float value)
    {
        while (true)
        {
            float current = location;
            float newValue = current + value;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (Interlocked.CompareExchange(ref location, newValue, current) == newValue)
                return newValue;
        }
    }
}
