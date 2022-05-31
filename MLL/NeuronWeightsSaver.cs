using Newtonsoft.Json;

namespace MLL;

public static class NeuronWeightsSaver
{
    private const string Filename = "sigmoidneurons.json";

    public static void Save(INeuron[] neurons)
    {
        var json = JsonConvert.SerializeObject(neurons);
        File.WriteAllText(Filename, json);
    }

    public static TNeuron[] Load<TNeuron>() where TNeuron : INeuron
    {
        var json = File.ReadAllText(Filename);
        return JsonConvert.DeserializeObject<TNeuron[]>(json)!;
    }
}
