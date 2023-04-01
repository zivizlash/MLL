namespace MLL.Repository.DataModels;

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
