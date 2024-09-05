using MLL.Common.Pooling;

namespace MLL.Network.Message.Protocol;

public struct RawMessage
{
    public ushort MessageType;
    public Pooled<byte[]> Data;
    //public byte[] Data;
    public int Length;
}
