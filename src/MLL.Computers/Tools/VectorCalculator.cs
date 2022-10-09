using System.Numerics;

namespace MLL.Computers.Tools;

public static class VectorCalculator
{
    public static float CalculateAbsSum(float[] arr)
    {
        var vectorSize = Vector<float>.Count;
        var accVector = Vector<float>.Zero;

        int i;

        for (i = 0; i < arr.Length - vectorSize; i += vectorSize)
        {
            var v1 = new Vector<float>(arr, i);
            accVector = Vector.Add(accVector, Vector.Abs(v1));
        }

        float sum = Vector.Sum(accVector);

        for (; i < arr.Length; i++)
            sum += MathF.Abs(arr[i]);

        return sum;
    }

    public static float CalculateMultiplySum(float[] arr1, float[] arr2)
    {
        if (arr1.Length != arr2.Length)
            throw new InvalidOperationException();

        var vectorSize = Vector<float>.Count;
        var accVector = Vector<float>.Zero;

        int i;

        for (i = 0; i < arr1.Length - vectorSize; i += vectorSize)
        {
            var v1 = new Vector<float>(arr1, i);
            var v2 = new Vector<float>(arr2, i);
            accVector = Vector.Add(accVector, Vector.Multiply(v1, v2));
        }

        float sum = Vector.Sum(accVector);

        for (; i < arr1.Length; i++)
            sum += arr1[i] * arr2[i];

        return sum;
    }
}
