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
