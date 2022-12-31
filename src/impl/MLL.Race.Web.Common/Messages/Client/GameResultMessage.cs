using MessagePack;

namespace MLL.Race.Web.Common.Messages.Client;

[MessagePackObject]
public class GameResultMessage
{
    [Key(0)]
    public float Score { get; set; }

    [Key(1)]
    public float Time { get; set; }
}
