using MLL.Network.Message.Handlers.Exceptions;
using System;
using System.Threading.Tasks;

namespace MLL.Network.Message.Handlers.Binding;

public class FuncMessageHandler<TTarget, TMessage> : IMessageHandler<TMessage>
{
    private readonly TTarget _target;
    private readonly Func<TTarget, TMessage, ValueTask> _handler;

    public FuncMessageHandler(TTarget target, Func<TTarget, TMessage, ValueTask> handler)
    {
        _target = target;
        _handler = async (target, msg) =>
        { 
            try
            {
                await handler.Invoke(target, msg);
            }
            catch (Exception ex)
            {
                throw new MessageHandlerExecutionException(ex);
            }
        };
    }

    public FuncMessageHandler(TTarget target, Func<TTarget, TMessage, Task> handler)
    {
        _target = target;
        _handler = async (target, msg) =>
        {
            try
            {
                await handler.Invoke(target, msg);
            }
            catch (Exception ex)
            {
                throw new MessageHandlerExecutionException(ex);
            }
        };
    }

    public FuncMessageHandler(TTarget target, Action<TTarget, TMessage> handler)
    {
        _target = target;
        _handler = (target, msg) => 
        {
            try
            {
                handler.Invoke(target, msg);
            }
            catch (Exception ex)
            {
                // Помнить, что ошибки в async методах в AggregationException заворачиваются
                throw new MessageHandlerExecutionException(ex);
            }

            return new ValueTask(); 
        };
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
