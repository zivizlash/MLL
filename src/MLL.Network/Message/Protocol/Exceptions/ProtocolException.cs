using System;

namespace MLL.Network.Message.Protocol.Exceptions;

public class ProtocolException : Exception
{
    public ProtocolException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
