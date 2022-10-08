namespace MLL.CUI.Models;

public class NetDataSource
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? DataFolder { get; set; }
    public DateTime Created { get; set; }

    public NetDataSource()
    {
        Created = DateTime.Now;
    }
}

public class NetUpdateInfo
{
    public int Id { get; set; }
    public TimeSpan LearningTime { get; set; }
    public DateTime StartDate { get; set; }
    public int EpochsCount { get; set; }

    public NetUpdateInfo()
    {
        StartDate = DateTime.UtcNow;
    }
}

public class NetLayerStructure
{
    public int Id { get; set; }
    public int LayerIndex { get; set; }
    public int WeightsCount { get; set; }
    public int NeuronsCount { get; set; }
    public bool IsShared { get; set; }
    public int SharedIndex { get; set; }
}

public enum NetSourceMode
{
    Undefined
}

public class NetInfo
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public NetDataSource? Source { get; set; }
    public NetSourceMode SourceMode { get; set; }
    public IList<NetLayerStructure> Structure { get; set; }
    public IList<NetUpdateInfo> Updates { get; set; }

    public NetInfo()
    {
        var now = DateTime.UtcNow;
        Created = now;
        Updated = now;

        Structure = new List<NetLayerStructure>();
        Updates = new List<NetUpdateInfo>();
    }
}
