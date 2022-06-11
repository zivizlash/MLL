using MLL.Builders;

namespace MLL.Neurons;

public class Net
{
    private NeuronLayer[] _layers;

    public NeuronLayer[] Layers
    {
        get => _layers;
        set => _layers = value;
    }

    public int OutputNeuronCount => _layers[^1].Count;

    public Net()
    {
        _layers = Array.Empty<NeuronLayer>();
    }
    
    public Net(float learningRate, params LayerDefinition[] definitions)
    {
        _layers = new NeuronLayer[definitions.Sum(d => d.Layers)];

        int index = 0;

        foreach (var definition in definitions)
        {
            for (int layer = 0; layer < definition.Layers; layer++)
            {
                _layers[index++] = new NeuronLayer(definition.NeuronsCount,
                    definition.WeightsCount, learningRate, definition.UseActivationFunc);
            }
        }
    }

    public Net UpdateLearningRate(float learningRate)
    {
        foreach (var layer in Layers)
            foreach (var neuron in layer.Neurons)
                neuron.LearningRate = learningRate;

        return this;
    }

    public float[] Predict(float[] input)
    {
        var output = input;

        foreach (var neuronLayer in _layers)
            output = neuronLayer.Predict(output);

        return output;
    }

    private float[][] PredictWithOutputs(float[] input)
    {
        var outputs = new float[_layers.Length][];
        var currentOutput = input;

        for (var i = 0; i < _layers.Length; i++)
        {
            var neuronLayer = _layers[i];
            currentOutput = neuronLayer.Predict(currentOutput);
            outputs[i] = currentOutput;
        }

        return outputs;
    }

    private static float[] GetLastLayerErrors(NeuronError[] errors)
    {
        var lastLayerErrors = new float[errors.Length];

        for (int i = 0; i < errors.Length; i++)
            lastLayerErrors[i] = errors[i].Error;

        return lastLayerErrors;
    }

    private static float[][] CreateNeuronInputModel(float[][] layersOutputs, float[] firstLayerInput)
    {
        var outputs = new float[layersOutputs.Length][];
        outputs[0] = firstLayerInput;

        for (int i = 0; i < layersOutputs.Length - 1; i++)
            outputs[i + 1] = layersOutputs[i];

        return outputs;
    }
    
    public float[] Train(float[] input, float[] expected)
    {
        var outputsWith = PredictWithOutputs(input);
        var outputs = CreateNeuronInputModel(outputsWith, input);

        // Количество ошибок последнего слоя = количеству нейронов.
        var errors = CalculateGeneralErrorsAndCompensate(_layers[^1], outputs[^1], expected);

        var lastLayerErrors = GetLastLayerErrors(errors);

        // Мы берем ошибку, и смотрим насколько текущие веса
        // влияют на конечный результат вычислений нейрона.
        for (int layerIndex = _layers.Length - 2; layerIndex >= 0; layerIndex--)
        {
            // errors тут хранит ошибки предпоследнего слоя.
            var layer = _layers[layerIndex];

            float[] localInput = outputs[layerIndex];
            float[] localOutput = outputs[layerIndex + 1];

            // Ошибки с последнего слоя. В данном случае количество элементов - 1.
            for (int i = 0; i < localOutput.Length; i++)
                errors[i].Output = localOutput[i];
            
            errors = CompensateAndReorganizeErrors(layer, errors, localInput);
        }

        return lastLayerErrors;
    }

    public Net FillRandomValues(Random random, double range = 0.5)
    {
        foreach (var layer in _layers)
        {
            foreach (var neuron in layer.Neurons)
                neuron.FillRandomValues(random, range);
        }

        return this;
    }

    private static NeuronError[] CompensateAndReorganizeErrors(NeuronLayer layer, NeuronError[] errors, float[] input)
    {
        int weightsCount = layer.Neurons[0].Weights.Length;
        var nextLayerErrors = new NeuronError[weightsCount];
        
        for (int i = 0; i < layer.Neurons.Length; i++)
        {
            var neuron = layer.Neurons[i];
            var error = errors[i];
            
            UpdateLayerErrors(neuron, error, nextLayerErrors);
            neuron.CompensateError(input, error);
        }

        return nextLayerErrors;
    }

    private static NeuronError[] CalculateGeneralErrorsAndCompensate(
        NeuronLayer layer, float[] previousInput, float[] expected)
    {
        var neurons = layer.Neurons;
        var weightsCount = neurons[0].Weights.Length;
        var nextLayerErrors = new NeuronError[weightsCount];
        
        for (int neuronIndex = 0; neuronIndex < neurons.Length; neuronIndex++)
        {
            var neuron = neurons[neuronIndex];
            var error = neuron.CalculateError(previousInput, expected[neuronIndex]);

            UpdateLayerErrors(neuron, error, nextLayerErrors);
            neuron.CompensateError(previousInput, error);
        }
        
        return nextLayerErrors;
    }

    private static void UpdateLayerErrors(SigmoidNeuron neuron,
        NeuronError currentLayerError, NeuronError[] nextLayerErrors)
    {
        var weightsSum = neuron.CalculateWeightsSum();

        for (int weightIndex = 0; weightIndex < neuron.Weights.Length; weightIndex++)
        {
            var weightValue = neuron.Weights[weightIndex];
            var errorPart = Math.Abs(weightValue) / weightsSum * currentLayerError.Error;
            nextLayerErrors[weightIndex].Error += errorPart;
        }
    }
}
