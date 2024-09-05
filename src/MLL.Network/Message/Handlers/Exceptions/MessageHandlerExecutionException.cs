using System;

namespace MLL.Network.Message.Handlers.Exceptions;

public class MessageHandlerExecutionException : Exception
{
    public MessageHandlerExecutionException(Exception ex) 
        : base("Error during execution message handler. See internal exception.", ex)
    {
    }
}
