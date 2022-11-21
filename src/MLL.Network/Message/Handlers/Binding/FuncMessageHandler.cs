using System;
using System.Threading.Tasks;

namespace MLL.Network.Message.Handlers.Binding;

public class FuncMessageHandler<TTarget, TMessage> : IMessageHandler<TMessage>
{
    // Тут куча ссылок плодиться будет на объект что замедляет сборку мусора
    // впринципе пофиг но не пофиг
    private readonly TTarget _target;
    private readonly Func<TTarget, TMessage, ValueTask> _handler;

    public FuncMessageHandler(TTarget target, Func<TTarget, TMessage, ValueTask> handler)
    {
        _target = target;
        _handler = handler;
    }

    public FuncMessageHandler(TTarget target, Func<TTarget, TMessage, Task> handler)
    {
        _target = target;
        _handler = (target, msg) => new ValueTask(handler.Invoke(target, msg));
    }

    public FuncMessageHandler(TTarget target, Action<TTarget, TMessage> handler)
    {
        _target = target;
        _handler = (target, msg) => { handler.Invoke(target, msg); return new ValueTask(); };
    }

    public ValueTask HandleAsync(TMessage message)
    {
        return _handler.Invoke(_target, message);
    }

    public ValueTask HandleAsync(object message)
    {
        if (message is not TMessage msg)
        {
            throw new ArgumentException(
                $"Invalid argument type: {message.GetType()}.", nameof(message));
        }

        return HandleAsync(msg);
    }
}
