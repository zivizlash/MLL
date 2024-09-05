using System;

namespace MLL.Network.Message.Protocol.Exceptions;

public class InvalidHeadersException : Exception
{
    public InvalidHeadersException(string message) : base(message)
    {
    }
}
