using MessagePack;

namespace MLL.Race.Web.Common.Messages.Client;

[MessagePackObject]
public class GameFrameMessage
{
    [Key(0)]
    public byte[]? Frame { get; set; }

    [Key(1)]
    public float ElapsedTime { get; set; }
}
