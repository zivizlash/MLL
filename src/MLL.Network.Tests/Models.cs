namespace MLL.Network.Tests;

public class PingMessage
{
    public int PingValue { get; set; }
}

public class PongMessage
{
    public int PongValue { get; set; }
    public int PongSquareValue { get; set; }
}

public class TestMessage
{
    public int IntValue { get; set; }
    public string? StringValue { get; set; }
}
