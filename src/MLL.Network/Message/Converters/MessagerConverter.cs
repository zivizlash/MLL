using MLL.Network.Message.Converters.Exceptions;
using MLL.Network.Message.Protocol;
using MsgPack;
using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MLL.Network.Message.Converters;

public class MessageConverter
{
    private readonly Dictionary<ushort, Type> _typeIdToType;
    private readonly Dictionary<Type, ushort> _typeToTypeId;

#pragma warning disable CS0618 // Type or member is obsolete
    private readonly Dictionary<Type, IMessagePackSingleObjectSerializer> _serializers;
#pragma warning restore CS0618 // Type or member is obsolete

    public MessageConverter(IEnumerable<Type> acceptableTypes, ProtocolVersionHashCode hashCode)
    {
        _typeIdToType = new();
        _typeToTypeId = new();

        var comparer = new HashCodeComparer(hashCode);
        var sorted = acceptableTypes.OrderBy(x => x, comparer).ToList();

        _serializers = new(sorted.Count);

        foreach (var type in sorted)
        {
            _serializers[type] = MessagePackSerializer.Get(type);
        }

        for (int i = 0; i < sorted.Count; i++)
        {
            _typeIdToType.Add((ushort)i, sorted[i]);
            _typeToTypeId.Add(sorted[i], (ushort)i);
        }
    }

    public (byte[], ushort) Serialize<T>(T obj)
    {
        //MessagePackSerializer.Get<T>();

        return (MessagePackSerializer.Get<T>().PackSingleObject(obj), _typeToTypeId[typeof(T)]);
    }

    public object Deserialize(byte[] bytes, int start, int length, ushort messageType)
    {
        if (start != 0)
        {
            throw new NotSupportedException($"Only zero value {nameof(start)} parameter supported at moment.");
        }

        if (!_typeIdToType.TryGetValue(messageType, out var type))
        {
            ThrowMessageTypeNotFound(messageType);
        }

        try
        {
            var arr = new byte[length];

            for (int i = 0, j = start; i < length; i++, j++)
            {
                arr[i] = bytes[j];
            }

            var serializer = _serializers[type];
            var result = serializer.UnpackSingleObject(arr);

            if (result == null)
            {
                throw new MessageSerializationException("Can't deserialize object");
            }

            return result;
        }
        catch (System.Runtime.Serialization.SerializationException ex)
        {
            throw new MessageSerializationException(ex);
        }
        catch (MessageTypeException ex)
        {
            throw new MessageSerializationException(ex);
        }
        catch (InvalidMessagePackStreamException ex)
        {
            throw new MessageSerializationException(ex);
        }
    }

    private static void ThrowMessageTypeNotFound(ushort messageType)
    {
        throw new MessageTypeNotFoundException($"MessageType {messageType} not registered");
    }
}
