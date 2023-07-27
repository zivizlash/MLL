using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using MLL.Computers.Tools;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MLL.Benchmark;

public static class Tools
{
    public static double[] GetRandomArray(Random rnd, int count)
    {
        var data = new double[count];

        for (int i = 0; i < data.Length; i++)
            data[i] = rnd.NextDouble();

        return data;
    }

    public static float[] GetSingleRandomArray(Random rnd, int count)
    {
        var data = new float[count];

        for (int i = 0; i < data.Length; i++)
            data[i] = rnd.NextSingle();

        return data;
    }
}

public class NaiveVsVectorBench
{
    private readonly double[] _input;
    private readonly double[] _weights;

    public NaiveVsVectorBench()
    {
        var rnd = new Random(42);
        _input = Tools.GetRandomArray(rnd, 16 * 1024);
        _weights = Tools.GetRandomArray(rnd, 16 * 1024);
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
    public double VectorTest2()
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
    public unsafe double Avx2Test()
    {
        int vectorSize = 256 / (Marshal.SizeOf<double>() * 8);
        var accVector = Vector256<double>.Zero;

        int i;
        var weightsArr = _weights;
        var inputArr = _input;

        //Avx2.LoadVector256()

        fixed (double* weightsPtr = weightsArr)
        {
            fixed (double* inputPtr = inputArr)
            {
                for (i = 0; i < weightsArr.Length - vectorSize; i++)
                {
                    var v1 = Avx2.LoadVector256(weightsPtr + i);
                    var v2 = Avx2.LoadVector256(inputPtr + i);

                    accVector = Avx2.Add(accVector, Avx2.Multiply(v1, v2));
                }
            }
        }

        double result = 0;

        var temp = stackalloc double[vectorSize];

        Avx2.Store(temp, accVector);
        
        for (int j = 0; j < vectorSize; j++)
            result += temp[j];

        for (; i < weightsArr.Length; i++)
            result += weightsArr[i];

        return result;
    }

    [Benchmark]
    public double VectorTest1()
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

public class NaiveVsVectorizedSubstractBench
{
    private readonly float[] _actual;
    private readonly float[] _expected;
    private readonly float[] _results;

    public NaiveVsVectorizedSubstractBench()
    {
        var rnd = new Random(42);
        var count = 16 * 1024 * 1024;

        _actual = Tools.GetSingleRandomArray(rnd, count);
        _expected = Tools.GetSingleRandomArray(rnd, count);
        _results = new float[count];
    }

    [Benchmark(Baseline = true)]
    public float[] Naive()
    {
        for (int i = 0; i < _results.Length; i++)
        {
            _results[i] = _expected[i] - _actual[i];
        }

        return _results;
    }

    [Benchmark]
    public void Vectorized()
    {
        VectorCalculator.Substract(_actual, _expected, _results, 0, _actual.Length);
    }
}

public class BenchConfig : ManualConfig
{
    public BenchConfig()
    {
        AddJob(Job.Default.WithId("Default"));

        AddJob(Job.Default.WithId("Dynamic PGO")
            .WithEnvironmentVariables(
                new EnvironmentVariable("DOTNET_TieredPGO", "1"),
                new EnvironmentVariable("DOTNET_TC_QuickJitForLoops", "1"),
                new EnvironmentVariable("DOTNET_ReadyToRun", "0")));
    }
}

public class Program
{
    public static void Main()
    {
        //var config = ManualConfig.Union(DefaultConfig.Instance, new BenchConfig());
        //BenchmarkRunner.Run<NaiveVsVectorBench>(config);

        BenchmarkRunner.Run<GpuVsCpu>();

        //BenchmarkRunner.Run<NaiveVsVectorizedSubstractBench>(DefaultConfig.Instance);
    }
}
