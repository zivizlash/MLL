using System.Runtime.CompilerServices;

namespace MLL;

public static class NumberTools
{
    public const double E = 2.7182818284590451;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Sigmoid(double value)
    {
        return 1.0 / (1.0 + Math.Pow(E, -value));
    }
}
