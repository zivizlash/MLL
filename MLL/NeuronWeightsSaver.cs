using Newtonsoft.Json;

namespace MLL;

public static class NeuronWeightsSaver
{
    public static void Save(INeuron[] neurons)
    {
        var json = JsonConvert.SerializeObject(neurons);
        File.WriteAllText("neurons.json", json);
    }

    public static TNeuron[] Load<TNeuron>() where TNeuron : INeuron
    {
        var json = File.ReadAllText("neurons.json");
        return JsonConvert.DeserializeObject<TNeuron[]>(json)!;
    }
}
