using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MLL.Network.Message.Handlers;

public class MessageHandlerBusBase : IMultiMessageHandler
{
    private readonly Dictionary<Type, IMessageHandler> _handlers;

    public IEnumerable<Type> AcceptableTypes => _handlers.Keys;

    public MessageHandlerBusBase()
    {
        _handlers = new();
    }

    public void AddHandler<TMessage>(Action<TMessage> handler)
    {
        _handlers.Add(typeof(TMessage), new ActionMessageHandler<TMessage>(msg =>
        {
            handler.Invoke(msg);
            return new ValueTask();
        }));
    }

    public void AddHandler<TMessage>(Func<TMessage, ValueTask> handler)
    {
        _handlers.Add(typeof(TMessage), new ActionMessageHandler<TMessage>(handler));
    }

    public ValueTask HandleAsync(object message)
    {
        return _handlers[message.GetType()].HandleAsync(message);
    }
}
