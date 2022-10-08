using Newtonsoft.Json;
using System.Text;

namespace MLL.CUI.Saving;

public static class PathHelper
{
    private static readonly HashSet<char> InvalidChars =
        Path.GetInvalidPathChars().ToHashSet();

    public static string NormalizeFolderPath(string path)
    {
        var sb = new StringBuilder(path.Length);

        foreach (var ch in path)
        {
            if (!InvalidChars.Contains(ch))
                sb.Append(ch);
        }

        return sb.ToString();
    }
}

public class NetSaveInfo
{
    public string? Name { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}

public class NetSaverConsts
{
    public static string NetSavesFilepath = "net_saves.json";

    private NetSaverConsts()
    {
    }
}

public class NetSaveDataSource
{
    public string? Folder { get; set; }
}

public class NetSaveLastAction
{

}

public class NetSave
{
    public List<NetSaveInfo> NetSaves { get; }
    public NetSaveLastAction? LastAction { get; set; }

    public NetSave()
    {
        NetSaves = new List<NetSaveInfo>();
    }
}

public class NetSaver
{
    private readonly string _baseFolder;
    private NetSaveInfo[]? _netSaves;

    public NetSaver(string folder)
    {
        Directory.CreateDirectory(folder);
        _baseFolder = folder;
    }

    public IReadOnlyList<NetSaveInfo> GetNetSaves() =>
        _netSaves ??= LoadOrDefault<NetSaveInfo[]>(NetSaverConsts.NetSavesFilepath) ?? new NetSaveInfo[0];

    private T? LoadOrDefault<T>(string filepath)
    {
        var fullpath = Path.Combine(_baseFolder, filepath);

        if (!File.Exists(fullpath))
            return default;

        var json = File.ReadAllText(fullpath);
        return JsonConvert.DeserializeObject<T>(json);
    }
}
