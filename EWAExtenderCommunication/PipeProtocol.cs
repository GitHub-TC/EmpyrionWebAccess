#nullable enable
using System;
using System.IO;
using ProtoBuf;

namespace EWAExtenderCommunication
{
    internal enum PipeMessageType : int
    {
        EmpyrionGameEventData = 1,
        ClientHostComData     = 2,
    }

    internal static class PipeProtocol
    {
        public static void WriteMessage(Stream stream, object message)
        {
            PipeMessageType tag = message switch
            {
                EmpyrionGameEventData => PipeMessageType.EmpyrionGameEventData,
                ClientHostComData     => PipeMessageType.ClientHostComData,
                _                     => throw new NotSupportedException($"Unknown message type: {message.GetType()}")
            };

            using var payload = new MemoryStream();
            Serializer.NonGeneric.Serialize(payload, message);
            var payloadBytes = payload.ToArray();

            using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
            writer.Write((int)tag);
            writer.Write(payloadBytes.Length);
            writer.Write(payloadBytes);
            stream.Flush();
        }

        public static object? ReadMessage(Stream stream)
        {
            using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            int tag;
            try { tag = reader.ReadInt32(); }
            catch (EndOfStreamException) { return null; }

            int length = reader.ReadInt32();
            if (length <= 0 || length > 64 * 1024 * 1024) return null;

            var buffer = reader.ReadBytes(length);
            if (buffer.Length != length) return null;

            using var ms = new MemoryStream(buffer);
            return (PipeMessageType)tag switch
            {
                PipeMessageType.EmpyrionGameEventData => Serializer.Deserialize<EmpyrionGameEventData>(ms),
                PipeMessageType.ClientHostComData     => Serializer.Deserialize<ClientHostComData>(ms),
                _                                     => null
            };
        }
    }
}
