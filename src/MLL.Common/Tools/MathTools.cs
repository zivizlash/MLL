namespace MLL.Common.Tools;

public static class MathTools
{
    public static float GetCloseness(float actual, float original, float power)
    {
        return 1.0f - (float)Math.Pow(Math.Abs(original - actual), power);
    }
}
