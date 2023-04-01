using MLL.Common.Tools;
using MLL.Repository.Tools;
using Newtonsoft.Json;

namespace MLL.Repository.Data;

public class NetDataFolder : INetData
{
    private readonly DirectoryInfo _dir;

    public NetDataFolder(string folder)
    {
        _dir = Directory.CreateDirectory(folder);
    }

    public T? GetOrDefault<T>() where T : class
    {
        try
        {
            var json = File.ReadAllText(GetFilePath<T>());
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    public void Set<T>(T value) where T : class
    {
        File.WriteAllText(GetFilePath<T>(), JsonConvert.SerializeObject(value));
    }

    private string GetFilePath<T>() where T : class
    {
        if (!ModelData<T>.IsNameValid)
        {
            Throw.Argument("Class name has invalid characters.", ModelData<T>.Name);
        }

        return Path.Combine(_dir.FullName, ModelData<T>.Name + ".json");
    }
}
