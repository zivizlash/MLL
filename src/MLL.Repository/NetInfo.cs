using MLL.Common.Engines;
using MLL.Repository.Data;
using MLL.Repository.DataModels;
using MLL.Repository.Tools;

namespace MLL.Repository;

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
        _globalData = new NetDataFolder(Path.Combine(folder, "settings")).WithCacheAndDateTimeUpdate();

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
