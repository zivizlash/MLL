using MLL.Common.Engines;
using MLL.Common.Factory;
using MLL.Common.Layer;
using MLL.Common.Tools;
using Newtonsoft.Json;
using System.Collections.Immutable;

namespace MLL.Repository;

public interface INetSnapshot
{
    INetData Data { get; }
    NetWeights Weights { get; set; }
}

public abstract class NetInfoFillerNetFactory : RandomFillerNetFactory
{
    private readonly INetInfo _netInfo;

    protected NetInfoFillerNetFactory(INetInfo netInfo, int randomSeed) : base(randomSeed)
    {
        _netInfo = netInfo;
    }

    public override void PostCreation(ClassificationEngine net)
    {
        if (_netInfo.HasSnapshots)
        {
            net.Weights = _netInfo.Latest.Weights;
            return;
        }

        base.PostCreation(net);
    }
}

internal static class PathValidator
{
    private static readonly ImmutableHashSet<char> _invalidChars;

    public static bool IsValidFileName(string fileName)
    {
        return !fileName.Any(_invalidChars.Contains);
    }

    static PathValidator()
    {
        _invalidChars = Path.GetInvalidFileNameChars().ToImmutableHashSet();
    }
}

internal static class ModelData<T> where T : class
{
    public static Type Type { get; }
    public static string Name { get; }
    public static bool IsNameValid { get; }

    static ModelData()
    {
        Type = typeof(T);
        Name = Type.Name;
        IsNameValid = PathValidator.IsValidFileName(Name);
    }
}

public interface INetData
{
    T? GetOrDefault<T>() where T : class;
    void Set<T>(T value) where T : class;
}

public interface INetInfo
{
    string Name { get; set; }
    string Description { get; set; }
    DateTime Created { get; }
    DateTime Updated { get; }
    INetData Data { get; }

    bool HasSnapshots { get; }

    IEnumerable<INetSnapshot> Snapshots { get; }
    INetSnapshot Latest { get; }

    INetSnapshot AddSnapshot(NetWeights weights);
}

public static class NetDataExtensions
{
    public static T Get<T>(this INetData netData) where T : class =>
        netData.GetOrDefault<T>() ?? throw new KeyNotFoundException();

    public static T GetOrDefault<T>(this INetData netData, T defaultValue) where T : class =>
        netData.GetOrDefault<T>() ?? defaultValue;

    public static T GetOrDefault<T>(this INetData netData, Func<T> defaultFactory) where T : class =>
        netData.GetOrDefault<T>() ?? defaultFactory.Invoke();

    public static T GetOrNew<T>(this INetData netData) where T : class, new() =>
        netData.GetOrDefault<T>() ?? new T();

    public static bool Has<T>(this INetData netData) where T : class =>
        netData.GetOrDefault<T>() != null;

    public static void NewOrUpdate<T>(this INetData netData, Action<T> action)
        where T : class, new()
    {
        var data = netData.GetOrNew<T>();
        action.Invoke(data);
        netData.Set(data);
    }

    public static INetData WithCache(this INetData netData) =>
        new NetDataCache(netData);

    public static INetData WithUpdateDateTime(this INetData netData) =>
        new NetDataUpdateDate(netData);

    public static INetData WithCacheAndDateTimeUpdate(this INetData netData) =>
        netData.WithCache().WithUpdateDateTime();
}

public class NetInfoLastUpdateNetData
{
    public DateTime LastUpdate { get; set; }

    public NetInfoLastUpdateNetData()
    {
        LastUpdate = DateTime.Now;
    }
}

public class NetDataUpdateDate : INetData
{
    private readonly INetData _netData;

    public NetDataUpdateDate(INetData netData)
    {
        _netData = netData;
    }

    public T? GetOrDefault<T>() where T : class
    {
        return _netData.GetOrDefault<T>();
    }

    public void Set<T>(T value) where T : class
    {
        _netData.Set(value);
        _netData.Set(new NetInfoLastUpdateNetData());
    }
}

public class NetDataCache : INetData
{
    private readonly INetData _netData;
    private readonly Dictionary<Type, object?> _cache;

    public NetDataCache(INetData netData)
    {
        _netData = netData;
        _cache = new();
    }

    public T? GetOrDefault<T>() where T : class
    {
        if (_cache.TryGetValue(ModelData<T>.Type, out var cached))
        {
            return (T?)cached;
        }

        var value = _netData.GetOrDefault<T>();
        _cache[ModelData<T>.Type] = value;
        return value;
    }

    public void Set<T>(T value) where T : class
    {
        _netData.Set(value);
        _cache[ModelData<T>.Type] = value;
    }
}

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

public class NetSnapshot : INetSnapshot
{
    private readonly string _folder;

    private DirectoryInfo? _dir;
    private INetData? _data;

    private DirectoryInfo Dir => _dir ??= Directory.CreateDirectory(_folder);

    public INetData Data => _data ??= new NetDataFolder(
        Path.Combine(Dir.FullName, "settings")).WithCacheAndDateTimeUpdate();

    public NetWeights Weights
    { 
        get => GetWeights();
        set => SaveWeights(value);
    }

    public NetSnapshot(string folder)
    {
        _folder = folder;
    }

    private NetWeights GetWeights()
    {
        var serializer = new JsonSerializer();

        try
        {
            var stream = File.OpenText(Path.Combine(Dir.FullName, "weights.json"));
            using var jsonReader = new JsonTextReader(stream);

            var value = serializer.Deserialize<WritableNet>(jsonReader);

            if (value == null)
            {
                return default;
            }

            var weights = new LayerWeights[value.Layers.Length];

            for (int i = 0; i < value.Layers.Length; i++)
            {
                weights[i] = new LayerWeights(value.Layers[i].Weights);
            }

            return new NetWeights(weights);
        }
        catch (FileNotFoundException)
        {
            return default;
        }
    }

    public void SaveWeights(NetWeights weights)
    {
        var serializer = new JsonSerializer();
        var filePath = Path.Combine(Dir.FullName, "weights.json");

        using var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        using var textWriter = new StreamWriter(fileStream);
        fileStream.Position = 0;

        serializer.Serialize(textWriter, weights);
        fileStream.SetLength(fileStream.Position);
    }

    private class WritableNet
    {
        public WritableWeights[] Layers = Array.Empty<WritableWeights>();
    }

    private class WritableWeights
    {
        public float[][] Weights = Array.Empty<float[]>();
    }
}

public class NetDatabase
{
    private readonly DirectoryInfo _netsDirectory;

    public NetDatabase(string folder)
    {
        _netsDirectory = Directory.CreateDirectory(folder).CreateSubdirectory("nets");
    }

    public INetInfo OpenOrCreate(NetCreation creation, Action<INetInfo> onCreatedAction)
    {
        var netPath = Path.Combine(_netsDirectory.FullName, ConvertNameToFolderName(creation.Name));
        var netInfo = new NetInfo(netPath);

        if (netInfo.IsJustCreated)
        {
            onCreatedAction.Invoke(netInfo);
        }

        return netInfo;
    }

    private static string ConvertNameToFolderName(string name)
    {
        var folderName = string.Join("", name
            .Select(ch => ch == ' ' ? '_' : ch)
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '_'));

        if (string.IsNullOrEmpty(folderName))
        {
            Throw.Argument(nameof(NetCreation.Name), "Name must contains at least one letter, digit or space char");
        }

        return folderName;
    }
}

public readonly struct NetCreation
{
    public readonly string Name;

    public NetCreation(string name)
    {
        Name = name;
    }
}

public class NetInfo : INetInfo
{
    private readonly DirectoryInfo _snapshotsDir;
    private readonly INetData _globalData;

    public string Name 
    { 
        get => _globalData.GetOrNew<NetInfoNameData>().Name;
        set => _globalData.NewOrUpdate<NetInfoNameData>(data => data.Name = value); 
    }

    public string Description
    {
        get => _globalData.GetOrNew<NetInfoNameData>().Description;
        set => _globalData.NewOrUpdate<NetInfoNameData>(data => data.Description = value);
    }

    public DateTime Created => _globalData.Get<NetInfoTimeData>().Created;
    public DateTime Updated => _globalData.Get<NetInfoTimeData>().Updated;
    public INetData Data => _globalData;

    public bool HasSnapshots => ReadSnapshots().Any();
    public bool IsJustCreated => _globalData.Has<NetInfoNameData>();

    public IEnumerable<INetSnapshot> Snapshots => ReadSnapshots();
    public INetSnapshot Latest => ReadSnapshots().First();

    public NetInfo(string folder)
    {
        _snapshotsDir = Directory.CreateDirectory(folder).CreateSubdirectory("snapshots");
        _globalData = new NetDataCache(new NetDataFolder(Path.Combine(folder, "settings")));

        EnsureTimeData(_globalData);
    }

    private IEnumerable<INetSnapshot> ReadSnapshots()
    {
        return _snapshotsDir
            .EnumerateDirectories()
            .OrderByDescending(d => d.Name.Length)
            .ThenByDescending(d => d.Name)
            .Select(folder => new NetSnapshot(folder.FullName));
    }

    public INetSnapshot AddSnapshot(NetWeights weights)
    {
        return new NetSnapshot(GetNewSnapshotFolderName())
        {
            Weights = weights
        };
    }

    private static void EnsureTimeData(INetData netData)
    {
        netData.Set(netData.GetOrNew<NetInfoTimeData>());
    }

    private string GetNewSnapshotFolderName()
    {
        var dirName = _snapshotsDir
            .EnumerateDirectories()
            .Where(dir => int.TryParse(dir.Name, out _))
            .Select(dir => int.Parse(dir.Name))
            .OrderByDescending(val => val)
            .FirstOrDefault();

        return Path.Combine(_snapshotsDir.FullName, (dirName + 1).ToString());
    }
}

public class NetInfoNameData
{
    public string Name { get; set; }
    public string Description { get; set; }

    [Obsolete]
    public NetInfoNameData()
    {
        Name = string.Empty;
        Description = string.Empty;
    }

    public NetInfoNameData(string name, string description)
    {
        Name = name;
        Description = description;
    }
}

public class NetInfoTimeData
{
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }

    public NetInfoTimeData()
    {
        var now = DateTime.Now;
        Created = now;
        Updated = now;
    }
}
