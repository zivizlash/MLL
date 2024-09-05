using ILGPU;
using ILGPU.Runtime;
using MLL.Common.Tools;
using System;

namespace MLL.Cuda;

public interface ICudaComputer : IDisposable
{
    void Prepare(float[,] weights, float[] input);
    void Execute();
    void CopyOutput(float[] output);
}

public class VectorCudaComputer : ICudaComputer
{
    public Accelerator Accelerator { get; }

    private VectorComputerData? _computerData;

    public VectorCudaComputer(Accelerator accelerator)
    {
        Accelerator = accelerator;
    }

    public void Prepare(float[,] weights, float[] input)
    {
        CheckDisposed();

        _computerData?.Dispose();
        _computerData = new VectorComputerData(Accelerator, weights, input);
    }

    public void Execute()
    {
        CheckDisposed();
        CheckInitialized();

        var data = _computerData!.Value;

        var weightsBuffer = data.WeightsBuffer;
        var inputBuffer = data.InputBuffer;
        var outputBuffer = data.OutputBuffer;

        var kernel = Accelerator.LoadAutoGroupedStreamKernel<
            Index1D, 
            ArrayView2D<float, Stride2D.DenseX>, 
            ArrayView1D<float, Stride1D.Dense>, 
            ArrayView1D<float, Stride1D.Dense>>(
                TestMultiplyKernel
        );

        kernel.Invoke((Index1D)data.InputCount, weightsBuffer, inputBuffer, outputBuffer);

        Accelerator.Synchronize();
    }

    public static void AtomicTestMultiplyKernel(Index2D index, ArrayView2D<float, Stride2D.DenseX> neurons,
        ArrayView1D<float, Stride1D.Dense> input, ArrayView1D<float, Stride1D.Dense> output)
    {
        var globalIndex = Grid.GlobalIndex.X;

        //var result = neurons[index] * input[input];
    }

    public static void TestMultiplyKernel(Index1D index, ArrayView2D<float, Stride2D.DenseX> neurons,
        ArrayView1D<float, Stride1D.Dense> input, ArrayView1D<float, Stride1D.Dense> output)
    {
        float acc = 0.0f;

        for (int i = 0; i < neurons.IntExtent.Y; i++)
        {
            acc += neurons[index, i] * input[i];
        }

        output[index] = acc;
    }

    public void CopyOutput(float[] output)
    {
        CheckDisposed();
        CheckInitialized();

        var data = _computerData!.Value;
        Check.LengthEqual(data.InputCount, output.Length, nameof(output));

        data.OutputBuffer.CopyToCPU(output);

    }

    private void CheckInitialized()
    {
        if (_computerData == null)
        {
            throw new InvalidOperationException("Object must be initialized.");
        }
    }

    private bool _disposed;

    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(VectorCudaComputer));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _computerData?.Dispose();
        _disposed = true;
    }

    public struct VectorComputerData : IDisposable
    {
        public readonly MemoryBuffer2D<float, Stride2D.DenseX> WeightsBuffer;
        public readonly MemoryBuffer1D<float, Stride1D.Dense> InputBuffer;
        public readonly MemoryBuffer1D<float, Stride1D.Dense> OutputBuffer;
        public readonly int InputCount;

        public VectorComputerData(Accelerator accelerator, float[][] weights, float[] input)
        {
            //WeightsBuffer = accelerator
            //AcceleratorExtension.
        }

        public VectorComputerData(Accelerator accelerator, float[,] weights, float[] input)
        {
            WeightsBuffer = accelerator.Allocate2DDenseX(weights);
            InputBuffer = accelerator.Allocate1D(input);
            OutputBuffer = accelerator.Allocate1D<float, Stride1D.Dense>(weights.GetLength(0), default);
            InputCount = weights.GetLength(0);
        }

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            WeightsBuffer.Dispose();
            InputBuffer.Dispose();
            OutputBuffer.Dispose();
        }
    }
}

public class CudaComputingContext : IDisposable
{
    private bool _disposed;

    Context Context { get; }
    Device Device { get; }
    Accelerator Accelerator { get; }

    public CudaComputingContext(Context context, Device device, Accelerator accelerator)
    {
        Context = context;
        Device = device;
        Accelerator = accelerator;
    }

    public void Dispose()
    {
        if (_disposed) return;
    }
}

public class Class1
{

}
