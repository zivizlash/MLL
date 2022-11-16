using MessagePack;
using MLL.Network.Message.Protocol;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace MLL.Network.Message.Converters;

public class MessageConverter
{
    private readonly Dictionary<ushort, Type> _typeIdToType;
    private readonly Dictionary<Type, ushort> _typeToTypeId;

    public MessageConverter(IEnumerable<Type> acceptableTypes)
    {
        _typeIdToType = new();
        _typeToTypeId = new();

        var sorted = acceptableTypes
            .OrderBy(x => x, new HashCodeComparer(new()))
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            _typeIdToType.Add((ushort)i, sorted[i]);
            _typeToTypeId.Add(sorted[i], (ushort)i);
        }
    }

    public (byte[], ushort) Serialize<T>(T obj)
    {
        var buffer = new ArrayBufferWriter<byte>();

        var messagePackWriter = new MessagePackWriter(buffer);
        MessagePackSerializer.Serialize(ref messagePackWriter, obj);

        return (buffer.GetMemory().ToArray(), _typeToTypeId[typeof(T)]);
    }

    public object Deserialize(byte[] bytes, ushort messageType)
    {
        return MessagePackSerializer.Deserialize(_typeIdToType[messageType], bytes.AsMemory());
    }
}
