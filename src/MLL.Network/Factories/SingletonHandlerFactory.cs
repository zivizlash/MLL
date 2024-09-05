using MLL.Common.Tools;
using System;

namespace MLL.Network.Factories;

public class SingletonHandlerFactory<THandler> : IMessageHandlerFactory
{
    private readonly IMessageHandlerFactory _factory;

    public Type MessageHandlerType => _factory.MessageHandlerType;

    private THandler? _handler;

    public THandler Instance
    {
        get
        {
            if (_handler == null)
            {
                Throw.InvalidOperation("Handler not created yet.");
            }

            return _handler!;
        }
    }

    public SingletonHandlerFactory(IMessageHandlerFactory factory)
    {
        _factory = factory;
    }

    public object CreateMessageHandler(MessageHandlerFactoryContext context)
    {
        return _handler ??= (THandler)_factory.CreateMessageHandler(context);
    }
}
