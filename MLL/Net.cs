namespace MLL;

public class Net
{
    private readonly IImageDataSetProvider _dataSetProvider;
    private readonly Func<Neuron> _neuronFactory;

    public Net(IImageDataSetProvider dataSetProvider, Func<Neuron> neuronFactory)
    {
        _dataSetProvider = dataSetProvider;
        _neuronFactory = neuronFactory;
    }

    public void Train(int epochs)
    {

    }
}
