using System;

namespace MLL.Network.Message.Protocol.Exceptions;

public class InvalidMagicCodeException : Exception
{
    public InvalidMagicCodeException() : this("Invalid magic code.")
    {
    }

    public InvalidMagicCodeException(string message) : base(message)
    {
    }
}
