//using MessagePack;

namespace MLL.Network.Tests;

//[MessagePackObject]
public class PingMessage
{
    //[Key(0)]
    public int PingValue { get; set; }

    //[Key(1)]
    public int Count { get; set; }
}

//[MessagePackObject]
public class PongMessage
{
    //[Key(0)]
    public int PongValue { get; set; }

    //[Key(1)]
    public int PongSquareValue { get; set; }

    //[Key(2)]
    public int Count { get; set; }
}

//[MessagePackObject]
public class TestMessage
{
    //[Key(0)]
    public int IntValue { get; set; }

    //[Key(1)]
    public string? StringValue { get; set; }
}

//[MessagePackObject]
public class GameFrameMessage
{
    //[Key(0)]
    public byte[]? Frame { get; set; }

    //[Key(1)]
    public float ElapsedTime { get; set; }
}
