using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MLL.Network.Message.Handlers.Binding;

public class BinderMultiMessageHandler : IMultiMessageHandler
{
    private readonly Dictionary<Type, IMessageHandler> _handlers;

    public IEnumerable<Type> AcceptableTypes => _handlers.Keys;

    public BinderMultiMessageHandler(Dictionary<Type, IMessageHandler> handlers)
    {
        _handlers = handlers;
    }

    public ValueTask HandleAsync(object message)
    {
        if (_handlers.TryGetValue(message.GetType(), out var handler))
        {
            return handler.HandleAsync(message);
        }

        throw new InvalidOperationException();
    }
}
