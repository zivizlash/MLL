using MLL.Network.Message.Protocol;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace MLL.Network.Factories;

public interface IMessageHandlerFactory
{
    object CreateMessageHandler(MessageHandlerFactoryContext context);
    Type MessageHandlerType { get; }
}

public class ReflectionMessageHandlerFactory<THandler> : IMessageHandlerFactory
    where THandler : class
{
    public Type MessageHandlerType => typeof(THandler);

    private readonly Func<MessageHandlerFactoryContext, THandler> _handlerFactory;

    public ReflectionMessageHandlerFactory()
    {
        var handlerType = typeof(THandler);

        var ctorInfos = handlerType.GetConstructors()
            .Select(c => new { Ctor = c, Params = c.GetParameters() })
            .Where(x => x.Params.Length < 2)
            .OrderByDescending(x => x.Params.Length);

        foreach (var ctorInfo in ctorInfos)
        {
            var handlerFactory = Generate(ctorInfo.Ctor);

            if (handlerFactory != null)
            {
                _handlerFactory = handlerFactory;
                return;
            }
        }

        throw new InvalidOperationException(
            $"Constructor accepting {nameof(IMessageSender)} or empty constructor not found.");
    }

    public object CreateMessageHandler(MessageHandlerFactoryContext context)
    {
        return _handlerFactory.Invoke(context)!;
    }
    
    private Func<MessageHandlerFactoryContext, THandler>? Generate(ConstructorInfo ctor)
    {
        var parameters = ctor.GetParameters();
        var param = parameters.FirstOrDefault();

        var isAcceptingSender = param?.ParameterType == typeof(IMessageSender);

        if (parameters.Length != 0 && !isAcceptingSender)
        {
            return null;
        }

        var inputTypes = new[] { typeof(MessageHandlerFactoryContext) };
        var method = new DynamicMethod(string.Empty, typeof(THandler), inputTypes);

        var il = method.GetILGenerator();

        if (isAcceptingSender)
        {
            var contextType = typeof(MessageHandlerFactoryContext);
            var senderPropertyName = nameof(MessageHandlerFactoryContext.MessageSender);
            var senderProperty = contextType.GetProperty(senderPropertyName);

            il.Emit(OpCodes.Ldarga_S, (byte)0);
            il.Emit(OpCodes.Callvirt, senderProperty.GetGetMethod());
        }

        il.Emit(OpCodes.Newobj, ctor);
        il.Emit(OpCodes.Ret);

        var func = method.CreateDelegate(typeof(Func<MessageHandlerFactoryContext, THandler>));
        return (Func<MessageHandlerFactoryContext, THandler>)func;
    }

}

public readonly struct MessageHandlerFactoryContext
{
    public Guid Uid { get; }
    public IMessageSender MessageSender { get; }

    public MessageHandlerFactoryContext(IMessageSender messageSender, Guid uid)
    {
        MessageSender = messageSender;
        Uid = uid;
    }
}
