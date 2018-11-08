using Eleon.Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace EWAExtenderCommunication
{

    [Serializable]
    public class EmpyrionGameEventData
    {
        public static Dictionary<string, Type> EleonModdingTypes = AppDomain.CurrentDomain.GetAssemblies()
                               .SelectMany(t => t.GetTypes())
                               .Where(t => t.Namespace == "Eleon.Modding")
                               .ToDictionary(t => t.FullName);

        public CmdId eventId;
        public ushort seqNr;

        public string serializedDataType;
        public byte[] serializedData;

        class ProtoBufCall<T>
        {
            public void Serialize(Stream aStream, T aData) { ProtoBuf.Serializer.Serialize<T>(aStream, (T)aData); }
            public T Deserialize(Stream aStream) { return ProtoBuf.Serializer.Deserialize<T>(aStream); }
        }

        public void SetEmpyrionObject(object aObject)
        {
            if (aObject == null) return;

            try
            {
                using (var MemBuffer = new MemoryStream())
                {
                    Type TypedProtoBufCall = typeof(ProtoBufCall<>);
                    var serializedDataType = aObject.GetType();
                    this.serializedDataType = serializedDataType.FullName;
                    TypedProtoBufCall = TypedProtoBufCall.MakeGenericType(new[] { serializedDataType });

                    object ProtoBufCallInstance = Activator.CreateInstance(TypedProtoBufCall);
                    MethodInfo MI = TypedProtoBufCall.GetMethod("Serialize");
                    MI.Invoke(ProtoBufCallInstance, new[] { MemBuffer, aObject });

                    MemBuffer.Seek(0, SeekOrigin.Begin);
                    serializedData = MemBuffer.ToArray();
                }
            }
            catch (Exception Error)
            {
                Console.WriteLine($"SetEmpyrionObject:{Error}");
            }
        }

        public object GetEmpyrionObject()
        {
            try
            {
                if (serializedData == null) return null;
                if (!EleonModdingTypes.TryGetValue(serializedDataType, out Type EleonType))
                {
                    Console.WriteLine($"GetEmpyrionObject:?:{serializedDataType}");
                    return null;
                }

                using (var MemBuffer = new MemoryStream(serializedData))
                {
                    Type TypedProtoBufCall = typeof(ProtoBufCall<>);
                    TypedProtoBufCall = TypedProtoBufCall.MakeGenericType(new[] { EleonType });

                    object ProtoBufCallInstance = Activator.CreateInstance(TypedProtoBufCall);
                    MethodInfo MI = TypedProtoBufCall.GetMethod("Deserialize");
                    return MI.Invoke(ProtoBufCallInstance, new[] { MemBuffer });
                }
            }
            catch (Exception Error)
            {
                Console.WriteLine($"SetEmpyrionObject:{Error}");
                return null;
            }
        }
    }
}