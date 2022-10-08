using MLL.Layer.Computers.Sigmoid;
using MLL.Layer.Threading;
using MLL.Tools;

namespace MLL.Layer.Backpropagation;

public class ThreadedErrorBackpropagation : IErrorBackpropagation, IThreadedComputer
{
    public LayerThreadInfo ThreadInfo { get; set; }

    private float[][]? _threadedErrors;

    private readonly Action<int> _calculateAction;
    private CountdownEvent? _countdown;

    private BackpropContext _context;
    private int _threadNeuronCount;

    public ThreadedErrorBackpropagation()
    {
        ThreadInfo = new(1);
        _calculateAction = Calculate;
    }

    public void ReorganizeErrors(BackpropContext ctx, float[] errors)
    {
        var neurons = ctx.Neurons;

        Check.LengthEqual(neurons[0].Length, errors.Length, nameof(errors));
        Array.Clear(errors);

        int threadsCount = Math.Min(ThreadInfo.Threads - 1, neurons.Length);

        _context = ctx;
        _threadNeuronCount = neurons.Length / Math.Max(1, threadsCount);
        _countdown = new CountdownEvent(threadsCount);

        EnsureBuffers(threadsCount, errors.Length);

        for (int threadIndex = 0; threadIndex < threadsCount; threadIndex++)
        {
            ThreadPool.QueueUserWorkItem(_calculateAction, threadIndex, false);
        }

        int start = _threadNeuronCount * threadsCount;

        for (int neuronIndex = start; neuronIndex < neurons.Length; neuronIndex++)
        {
            var weights = neurons[neuronIndex];
            var error = ctx.Errors[neuronIndex];
            ErrorBackpropagationTools.CalculateNeuronError(weights, error, errors);
        }

        _countdown.Wait();

        for (int bufferIndex = 0; bufferIndex < threadsCount; bufferIndex++)
        {
            var buffer = _threadedErrors![bufferIndex];

            for (int ni = 0; ni < buffer.Length; ni++)
            {
                errors[ni] += buffer[ni];
            }
        }
    }

    private void Calculate(int index)
    {
        var errorsBuffer = _threadedErrors![index];
        Array.Clear(errorsBuffer);

        var neurons = _context.Neurons;
        var errors = _context.Errors;

        int start = index * _threadNeuronCount;
        int end = start + _threadNeuronCount;

        for (int neuronIndex = start; neuronIndex < end; neuronIndex++)
        {
            var weights = neurons[neuronIndex];
            var error = errors[neuronIndex];
            ErrorBackpropagationTools.CalculateNeuronError(weights, error, errorsBuffer);
        }

        _countdown!.Signal();
    }

    private void EnsureBuffers(int count, int itemsCount)
    {
        if (count == 0) return;

        if (_threadedErrors?.Length != count)
        {
            _threadedErrors = new float[count][];
        }

        if (_threadedErrors[0]?.Length != itemsCount)
        {
            for (int i = 0; i < count; i++)
            {
                _threadedErrors[i] = new float[itemsCount];
            }
        }
    }
}
