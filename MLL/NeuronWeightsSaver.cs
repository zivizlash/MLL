using Newtonsoft.Json;

namespace MLL;

public static class NeuronWeightsSaver
{
    private const string Filename = "sigmoid64x64weights.json";

    public static void Save(Neuron[] neurons)
    {
        var json = JsonConvert.SerializeObject(neurons);
        File.WriteAllText(Filename, json);
    }

    public static TNeuron[] Load<TNeuron>() where TNeuron : Neuron
    {
        var json = File.ReadAllText(Filename);
        return JsonConvert.DeserializeObject<TNeuron[]>(json)!;
    }
}
