using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MLL.Network.Message.Handlers.Binding;

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

        return (Func<object, IMessageHandler>)factory;
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
