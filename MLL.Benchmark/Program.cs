using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace MLL.Benchmark;

public class NaiveVsVectorBench
{
    private readonly double[] _input;
    private readonly double[] _weights;

    private static double[] GetRandomArray(Random rnd, int count)
    {
        var data = new double[count];

        for (int i = 0; i < data.Length; i++)
            data[i] = rnd.NextDouble();

        return data;
    }

    public NaiveVsVectorBench()
    {
        var rnd = new Random(42);
        _input = GetRandomArray(rnd, 16 * 1024);
        _weights = GetRandomArray(rnd, 16 * 1024);
    }

    [Benchmark]
    public double NaiveTest()
    {
        double result = 0;

        for (int i = 0; i < _input.Length; i++)
            result += _input[i] * _weights[i];

        return result;
    }

    [Benchmark]
    public double Vector2Test()
    {
        var vectorSize = Vector<double>.Count;
        
        int i;

        double acc = 0;

        for (i = 0; i < _weights.Length - vectorSize; i += vectorSize)
        {
            var v1 = new Vector<double>(_weights, i);
            var v2 = new Vector<double>(_input, i);
            acc += Vector.Sum(Vector.Multiply(v1, v2));
        }
        
        for (; i < _weights.Length; i++)
            acc += _weights[i] * _input[i];

        return acc;
    }

    [Benchmark]
    public double Vector1Test()
    {
        var vectorSize = Vector<double>.Count;
        var accVector = Vector<double>.Zero;
        
        int i;

        for (i = 0; i < _weights.Length - vectorSize; i += vectorSize)
        {
            var v1 = new Vector<double>(_weights, i);
            var v2 = new Vector<double>(_input, i);
            accVector = Vector.Add(accVector, Vector.Multiply(v1, v2));
        }

        double result = Vector.Sum(accVector);

        for (; i < _weights.Length; i++)
            result += _weights[i] * _input[i];

        return result;
    }
}

public class Program
{
    public static void Main()
    {
        BenchmarkRunner.Run<NaiveVsVectorBench>();
    }
}
