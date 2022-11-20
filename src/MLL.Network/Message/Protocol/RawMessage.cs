namespace MLL.Network.Message.Protocol;

public struct RawMessage
{
    public ushort MessageType;
    public byte[] Data;
}
