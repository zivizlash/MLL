using System;

namespace MLL.Network.Message.Converters.Exceptions;

public class MessageTypeNotFoundException : Exception
{

    public MessageTypeNotFoundException(string message, Exception inner) : base(message, inner)
    {
    }

    public MessageTypeNotFoundException(string message) : base(message)
    {
    }
}

public class MessageSerializationException : Exception
{
    private const string _defaultErrorMessage = "Error while serialization/deserialization. See inner message";

    public MessageSerializationException(string message) : base(message)
    {
    }

    public MessageSerializationException(string message, Exception inner) : base(message, inner)
    {
    }

    public MessageSerializationException(Exception inner) : base(_defaultErrorMessage, inner)
    {
    }
}
