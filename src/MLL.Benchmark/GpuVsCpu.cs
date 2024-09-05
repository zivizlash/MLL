using BenchmarkDotNet.Attributes;
using ILGPU;
using ILGPU.Runtime;
using MLL.Common.Tools;
using MLL.Computers.Layers.Sum;
using MLL.Cuda;

namespace MLL.Benchmark;

public class GpuVsCpu
{
    private VectorCudaComputer _vectorComputer;
    private SumPredictComputer _sumPredictComputer;
    private Context _context;
    private Accelerator _accelerator;

    private float[][] _cpuWeights;
    private float[] _cpuInput;
    private float[] _cpuOutput;

    [GlobalSetup]
    public void Setup()
    {
        _sumPredictComputer = new SumPredictComputer
        {
            ThreadInfo = new Common.Threading.LayerThreadInfo(8)
        };

        _context = Context.Create(x => x.AllAccelerators());
        var device = _context.GetPreferredDevice(preferCPU: false);
        _accelerator = device.CreateAccelerator(_context);

        Console.WriteLine(device);

        _vectorComputer = new VectorCudaComputer(_accelerator);

        int WEIGHTS_COUNT = 1000000;
        int NEURONS_COUNT = 2000;

        _cpuWeights = new float[NEURONS_COUNT][];

        var rnd = new Random(10006);

        _cpuInput = Tools.GetSingleRandomArray(rnd, WEIGHTS_COUNT);
        _cpuOutput = new float[NEURONS_COUNT];

        for (int i = 0; i < _cpuWeights.Length; i++)
        {
            _cpuWeights[i] = Tools.GetSingleRandomArray(rnd, WEIGHTS_COUNT);
        }

        var gpuData = new float[NEURONS_COUNT, WEIGHTS_COUNT];

        for (int i = 0; i < _cpuWeights.Length; i++)
        {
            var data = _cpuWeights[i];

            for (int j = 0; j < WEIGHTS_COUNT; j++)
            {
                gpuData[i, j] = data[j];
            }
        }

        _vectorComputer.Prepare(gpuData, _cpuInput);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _vectorComputer.Dispose();
        _accelerator.Dispose();
        _context.Dispose();
    }

    //[Benchmark]
    //public float[] VectorGpuWithCopyToCpu()
    //{
    //    _vectorComputer.Execute();
    //    _vectorComputer.CopyOutput(_cpuOutput);
    //    return _cpuOutput;
    //}

    [Benchmark]
    public void VectorGpu()
    {
        _vectorComputer.Execute();
    }

    [Benchmark(Baseline = true)]
    public float[] VectorCpu()
    {
        _sumPredictComputer.Predict(new(_cpuWeights), _cpuInput, _cpuOutput, ProcessingRange.From(_cpuOutput));
        return _cpuOutput;
    }
}
