using MessagePack;

namespace MLL.Race.Web.Common.Messages.Server;

[MessagePackObject]
public class CarMovementUpdateMessage
{
    [Key(0)]
    public float Forward { get; set; }

    [Key(1)]
    public float Left { get; set; }
}
