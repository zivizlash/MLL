using MLL.Neurons;
using Newtonsoft.Json;

namespace MLL.Saving;

public static class NeuronWeightsSaver
{
    private const string Filename = "hiddennetsig1.json";

    public static void Save(Net net)
    {
        var json = JsonConvert.SerializeObject(net);
        File.WriteAllText(Filename, json);
    }

    public static Net Load()
    {
        var json = File.ReadAllText(Filename);
        return JsonConvert.DeserializeObject<Net>(json)!;
    }
}
