//using MessagePack;
//using MessagePack.Resolvers;
using MLL.Network.Message.Protocol;
using MsgPack.Serialization;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace MLL.Network.Message.Converters;

public class MessageConverter
{
    private readonly Dictionary<ushort, Type> _typeIdToType;
    private readonly Dictionary<Type, ushort> _typeToTypeId;

    //private static readonly MessagePackSerializerOptions _options;

    private readonly Dictionary<Type, IMessagePackSingleObjectSerializer> _serializers;

    static MessageConverter()
    {
        //_options = MessagePackSerializer.DefaultOptions; // MessagePackSerializerOptions.Standard;
    }

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
        //var buffer = new ArrayBufferWriter<byte>();

        //var messagePackWriter = new MessagePackWriter(buffer);
        //MessagePackSerializer.Serialize(ref messagePackWriter, obj, _options);

        return (MessagePackSerializer.Get<T>().PackSingleObject(obj), _typeToTypeId[typeof(T)]);

        //MessagePackSerializer.Typeless.Serialize(ref messagePackWriter, obj);

        //return (buffer.GetMemory().ToArray(), _typeToTypeId[typeof(T)]);
    }

    public object Deserialize(byte[] bytes, ushort messageType)
    {
        //return MessagePackSerializer.Typeless.Deserialize(bytes.AsMemory());

        return _serializers[_typeIdToType[messageType]].UnpackSingleObject(bytes);

        //return MessagePackSerializer.Deserialize(
        //    _typeIdToType[messageType], bytes.AsMemory(), _options)!;
    }
}
