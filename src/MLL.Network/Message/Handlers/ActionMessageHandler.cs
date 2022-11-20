using System;
using System.Threading.Tasks;

namespace MLL.Network.Message.Handlers;

public class ActionMessageHandler<TMessage> : IMessageHandler<TMessage>
{
    private readonly Func<TMessage, ValueTask> _action;

    public ActionMessageHandler(Func<TMessage, ValueTask> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public ActionMessageHandler(Func<TMessage, Task> action)
    {
        _ = action ?? throw new ArgumentNullException(nameof(action));
        _action = msg => new ValueTask(action.Invoke(msg));
    }

    public ActionMessageHandler(Action<TMessage> action)
    {
        _ = action ?? throw new ArgumentException(nameof(action));
        _action = msg => { action.Invoke(msg); return new ValueTask(); };
    }

    public ValueTask HandleAsync(TMessage message)
    {
        return _action.Invoke(message);
    }

    public ValueTask HandleAsync(object message)
    {
        if (message is not TMessage msg)
        {
            throw new ArgumentException(nameof(message));
        }

        return _action.Invoke(msg);
    }
}
