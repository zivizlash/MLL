using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MLL.Network.Message.Handlers;

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

public class MessageHandlerDelegateFactoryCreator
{
    private readonly Dictionary<Type, MethodInfo> _factoryCreators;

    private static readonly Type[] _returnTypes;

    static MessageHandlerDelegateFactoryCreator()
    {
        _returnTypes = new[] { typeof(ValueTask), typeof(Task) };
    }

    public MessageHandlerDelegateFactoryCreator()
    {
        _factoryCreators = GetType()
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(mi => mi.Name == nameof(CreateFactory))
            .ToDictionary(GetDelegateArgumentReturnType);
    }

    public Func<object, IMessageHandler> CreateFactory(
        Type targetType, Type messageType, Delegate factoryDelegate)
    {
        var delegateType = factoryDelegate.GetType();
        var returnType = GetDelegateParameterReturnType(delegateType);

        if (!_factoryCreators.TryGetValue(returnType, out var factoryDef))
        {
            throw new InvalidOperationException(
                $"Unknown delegate signature {factoryDelegate}.");
        }

        var factoryCreatorMethod = factoryDef.MakeGenericMethod(targetType, messageType);
        var factory = factoryCreatorMethod.Invoke(this, new object[] { factoryDelegate });

        return (Func<object, IMessageHandler>) factory;
    }

    private static Type GetDelegateArgumentReturnType(MethodInfo methodInfo)
    {
        var parameterType = methodInfo.GetParameters().Single().ParameterType;
        return GetDelegateParameterReturnType(parameterType);
    }

    private static Type GetDelegateParameterReturnType(Type parameterType)
    {
        if (parameterType.GetGenericTypeDefinition() == typeof(Action<,>))
        {
            return typeof(void);
        }
        else
        {
            var returnType = parameterType.GetGenericArguments().Skip(2).Single();

            if (!_returnTypes.Contains(returnType))
            {
                throw new InvalidOperationException();
            }

            return returnType;
        }
    }

    private static Func<object, IMessageHandler> CreateFactory<TTarget, TMessage>(
        Action<TTarget, TMessage> action)
    {
        return target => new FuncMessageHandler<TTarget, TMessage>((TTarget)target, action);
    }

    private static Func<object, IMessageHandler> CreateFactory<TTarget, TMessage>(
        Func<TTarget, TMessage, Task> func)
    {
        return target => new FuncMessageHandler<TTarget, TMessage>((TTarget)target, func);
    }

    private static Func<object, IMessageHandler> CreateFactory<TTarget, TMessage>(
        Func<TTarget, TMessage, ValueTask> func)
    {
        return target => new FuncMessageHandler<TTarget, TMessage>((TTarget)target, func);
    }
}

public class AttributeMessageHandlerBinder
{
    private readonly Type[] _validReturnTypes;
    private readonly Dictionary<Type, MessageHandlerFactoryInfo[]> _factories;
    private readonly MessageHandlerDelegateFactoryCreator _factoryCreator;

    public AttributeMessageHandlerBinder()
    {
        _validReturnTypes = new[] { typeof(void), typeof(Task), typeof(ValueTask) };
        _factories = new();
        _factoryCreator = new();
    }

    public IMultiMessageHandler Bind(Type type, object target)
    {
        if (!_factories.TryGetValue(type, out var factories))
        {
            _factories[type] = factories = GenerateHandlerFactories(type);
        }

        var typeToHandler = new Dictionary<Type, IMessageHandler>(factories.Length);

        foreach (var factory in factories)
        {
            var handler = factory.Factory.Invoke(target);
            typeToHandler.Add(factory.MessageType, handler);
        }

        return new BinderMultiMessageHandler(typeToHandler);
    }

    private MessageHandlerFactoryInfo[] GenerateHandlerFactories(Type targetType)
    {
        var factories = new List<MessageHandlerFactoryInfo>();
        
        foreach (var method in targetType.GetMethods().Where(IsHandler))
        {
            var parameters = method.GetParameters();

            if (parameters.Length != 1)
            {
                throw new InvalidEnumArgumentException(
                    "Handlers require 1 parameter which is the message.");
            }

            if (!_validReturnTypes.Contains(method.ReturnType))
            {
                throw new InvalidOperationException(
                    "Handlers required void, Task or ValueTask type as return parameter");
            }

            var messageType = parameters.Single().ParameterType;
            Delegate outputDelegate;

            if (method.ReturnType == typeof(void))
            {
                var pattern = typeof(Action<,>).MakeGenericType(targetType, messageType);
                outputDelegate = method.CreateDelegate(pattern);
            }
            else
            {
                var pattern = typeof(Func<,,>).MakeGenericType(targetType, messageType, method.ReturnType);
                outputDelegate = method.CreateDelegate(pattern);
            }

            var funcFactory = _factoryCreator.CreateFactory(targetType, messageType, outputDelegate);
            factories.Add(new MessageHandlerFactoryInfo(funcFactory, messageType));
        }

        return factories.ToArray();
    }

    private static bool IsHandler(MethodInfo method)
    {
        return method.GetCustomAttribute<MessageHandlerAttribute>() != null;
    }

    private readonly struct MessageHandlerFactoryInfo
    {
        public readonly Func<object, IMessageHandler> Factory;
        public readonly Type MessageType;

        public MessageHandlerFactoryInfo(Func<object, IMessageHandler> factory, Type messageType)
        {
            Factory = factory;
            MessageType = messageType;
        }
    }

    private class BinderMultiMessageHandler : IMultiMessageHandler
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
}

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
