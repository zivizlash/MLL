using System.Runtime.CompilerServices;

namespace MLL;

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
}
