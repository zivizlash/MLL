using MLL.Neurons;
using Newtonsoft.Json;

namespace MLL.Saving;

public static class NeuronWeightsSaver
{
    private const string DirPath = "../../../Data/";
    private const string Filename = "/hiddennetsig1.json";
    
    public static void Save(Net net)
    {
        Directory.CreateDirectory(DirPath);
        var filename = DirPath + Filename;

        var json = JsonConvert.SerializeObject(net);
        File.WriteAllText(filename, json);
    }

    public static Net Load()
    {
        var filename = DirPath + Filename;
        var json = File.ReadAllText(filename);
        return JsonConvert.DeserializeObject<Net>(json)!;
    }
}
