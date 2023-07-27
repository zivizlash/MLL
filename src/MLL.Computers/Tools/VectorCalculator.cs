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
        {
            sum += MathF.Abs(arr[i]);
        }

        return sum;
    }

    public static void Substract(float[] left, float[] right, float[] result, int startIndex, int stopIndex)
    {
        if (right.Length != left.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(left));
        }

        if (right.Length != result.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(result));
        }

        var vectorSize = Vector<float>.Count;
        
        int i;

        for (i = startIndex; i < stopIndex - vectorSize; i += vectorSize)
        {
            var leftVector = new Vector<float>(left, i);
            var rightVector = new Vector<float>(right, i);

            Vector.Subtract(leftVector, rightVector).CopyTo(result, i);
        }

        for (; i < stopIndex; i++)
        {
            result[i] = left[i] - right[i];
        }
    }

    public static float CalculateMultiplySum(float[] arr1, float[] arr2)
    {
        if (arr1.Length != arr2.Length)
        {
            throw new InvalidOperationException();
        }

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
        {
            sum += arr1[i] * arr2[i];
        }

        return sum;
    }
}
