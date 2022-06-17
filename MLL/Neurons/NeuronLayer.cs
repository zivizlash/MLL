namespace MLL.Neurons;

public class NeuronLayer
{
    private SigmoidNeuron[] _neurons;

    private float[]? _buffer;
    
    public SigmoidNeuron[] Neurons
    {
        get => _neurons;
        set => _neurons = value;
    }

    public int Count => _neurons.Length;

    public NeuronLayer()
    {
        _neurons = Array.Empty<SigmoidNeuron>();
    }

    public NeuronLayer(int neuronsCount, int weightsCount, float learningRate, bool useActivationFunc)
    {
        _neurons = new SigmoidNeuron[neuronsCount];

        for (int i = 0; i < _neurons.Length; i++)
            _neurons[i] = new SigmoidNeuron(weightsCount, learningRate, useActivationFunc);
    }
    
    public float[] Predict(float[] input)
    {
        if (_buffer?.Length != _neurons.Length)
            _buffer = new float[_neurons.Length];
        
        for (var i = 0; i < _neurons.Length; i++)
        {
            var neuron = _neurons[i];
            _buffer[i] = neuron.Predict(input);
        }
    
        
        return _buffer;
    }
}
