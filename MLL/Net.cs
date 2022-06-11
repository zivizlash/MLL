namespace MLL;

public class Net
{
    private readonly NeuronLayer[] _layers;

    public int OutputNeuronCount { get; }
    
    public Net(double learningRate, params LayerDefinition[] definitions)
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

        OutputNeuronCount = _layers[^1].Count;
    }

    public double[] Predict(double[] input)
    {
        var output = input;

        foreach (var neuronLayer in _layers)
            output = neuronLayer.Predict(output);

        return output;
    }

    private double[][] PredictWithOutputs(double[] input)
    {
        var outputs = new double[_layers.Length][];
        var currentOutput = input;

        for (var i = 0; i < _layers.Length; i++)
        {
            var neuronLayer = _layers[i];
            currentOutput = neuronLayer.Predict(currentOutput);
            outputs[i] = currentOutput;
        }

        return outputs;
    }

    private static double[] GetLastLayerErrors(NeuronError[] errors)
    {
        var lastLayerErrors = new double[errors.Length];

        for (int i = 0; i < errors.Length; i++)
            lastLayerErrors[i] = errors[i].Error;

        return lastLayerErrors;
    }

    private static double[][] CreateNeuronInputModel(double[][] layersOutputs, double[] firstLayerInput)
    {
        var outputs = new double[layersOutputs.Length][];
        outputs[0] = firstLayerInput;

        for (int i = 0; i < layersOutputs.Length - 1; i++)
            outputs[i + 1] = layersOutputs[i];

        return outputs;
    }

    public void PrintWeights()
    {
        for (var layerIndex = 0; layerIndex < _layers.Length; layerIndex++)
        {
            var layer = _layers[layerIndex];

            Console.WriteLine($"Layer: {layerIndex}");

            foreach (var neuron in layer.Neurons)
            {
                Console.Write($"{string.Join(", ", neuron.Weights)};");
            }

            Console.WriteLine();
        }
    }

    public double[] Train(double[] input, double[] expected)
    {
        var outputsWith = PredictWithOutputs(input);
        var outputs = CreateNeuronInputModel(outputsWith, input);

        // Количество ошибок последнего слоя = количеству нейронов.
        var errors = CalculateLayerErrorsAndCompensate(_layers[^1], outputs[^1], expected);
        
        // Мы берем ошибку, и смотрим насколько текущие веса
        // влияют на конечный результат вычислений нейрона.
        for (int layerIndex = _layers.Length - 2; layerIndex >= 0; layerIndex--)
        {
            // errors тут хранит ошибки предпоследнего слоя.
            var layer = _layers[layerIndex];

            double[] localInput = outputs[layerIndex];
            double[] localOutput = outputs[layerIndex + 1];

            // Ошибки с последнего слоя. В данном случае количество элементов - 1.
            for (int i = 0; i < localOutput.Length; i++)
                errors[i].Output = localOutput[i];
            
            errors = CalculateAndCompensateErrors(layer, errors, localInput);
        }

        return outputsWith[^1];
    }

    public void FillRandomValues(Random random, double range = 0.5)
    {
        foreach (var layer in _layers)
        {
            foreach (var neuron in layer.Neurons)
                neuron.FillRandomValues(random, range);
        }
    }

    private static NeuronError[] CalculateAndCompensateErrors(NeuronLayer layer, NeuronError[] errors, double[] input)
    {
        // Тут мы считаем, что все нейроны имеет одинаковые 
        int weightsCount = layer.Neurons[0].Weights.Length;
        var nextLayerErrors = new NeuronError[weightsCount];
        
        // Мы берем ошибку, и смотрим насколько текущие веса
        // влияют на конечный результат вычислений нейрона.
        for (int i = 0; i < layer.Neurons.Length; i++)
        {
            var neuron = layer.Neurons[i];
            var error = errors[i];

            double weightsSum = neuron.CalculateWeightsSum();
            
            for (int weightIndex = 0; weightIndex < neuron.Weights.Length; weightIndex++)
            {
                var weightValue = neuron.Weights[weightIndex];
                var errorPart = weightValue / weightsSum * error.Error;
                nextLayerErrors[weightIndex].Error += errorPart;
            }
            
            // Так как у нас в зависимости есть входные данные, то нам надо их предоставить.
            // Получается так, что входные данные этого нейрона это выход предыдущих нейронов.
            neuron.CompensateError(input, error);
        }

        return nextLayerErrors;
    }

    private static NeuronError[] CalculateLayerErrorsAndCompensate(
        NeuronLayer layer, double[] previousInput, double[] expected)
    {
        var neurons = layer.Neurons;
        var weightsCount = neurons[0].Weights.Length;
        var errors = new NeuronError[weightsCount];
        
        for (int neuronIndex = 0; neuronIndex < neurons.Length; neuronIndex++)
        {
            var neuron = neurons[neuronIndex];
            var neuronExpected = expected[neuronIndex];

            var error = neuron.CalculateError(previousInput, neuronExpected);
            double weightsSum = neuron.CalculateWeightsSum();
            
            neuron.CompensateError(previousInput, error);
            
            for (int weightIndex = 0; weightIndex < weightsCount; weightIndex++)
            {
                double weightValue = neuron.Weights[weightIndex];
                double errorPart = Math.Abs(weightValue) / weightsSum * error.Error;
                errors[weightIndex].Error += errorPart;
                errors[weightIndex].Output = previousInput[weightIndex];
            }
        }
        
        return errors;
    }
}
