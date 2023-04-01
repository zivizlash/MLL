namespace MLL.Repository.DataModels;

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
