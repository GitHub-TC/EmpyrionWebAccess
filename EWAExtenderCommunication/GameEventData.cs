using Eleon.Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EWAExtenderCommunication
{

    [Serializable]
    public class EmpyrionGameEventData
    {
        private static Dictionary<string, Type> _mEleonModdingTypes;

        public static Dictionary<string, Type> EleonModdingTypes
        {
            get {
                if (_mEleonModdingTypes == null) _mEleonModdingTypes = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(t => t.GetTypes())
                        .Where(t => t.Namespace == "Eleon.Modding")
                        .ToDictionary(t => t.FullName);

                return _mEleonModdingTypes;
            }
        }

        public CmdId eventId;
        public ushort seqNr;

        byte[] serializedData;
        string serializedDataType;

        class ProtoBufCall<T>
        {
            public void Serialize(Stream aStream, T aData) { ProtoBuf.Serializer.Serialize<T>(aStream, (T)aData); }
            public T Deserialize(Stream aStream) { return ProtoBuf.Serializer.Deserialize<T>(aStream); }
        }

        public void SetEmpyrionObject(object data)
        {
            if (data == null) return;

            try
            {
                using (var MemBuffer = new MemoryStream())
                {
                    Type TypedProtoBufCall = typeof(ProtoBufCall<>);
                    serializedDataType = data.GetType().FullName;
                    TypedProtoBufCall = TypedProtoBufCall.MakeGenericType(new[] { data.GetType() });

                    object ProtoBufCallInstance = Activator.CreateInstance(TypedProtoBufCall);
                    MethodInfo MI = TypedProtoBufCall.GetMethod("Serialize");
                    MI.Invoke(ProtoBufCallInstance, new[] { MemBuffer, data });

                    MemBuffer.Seek(0, SeekOrigin.Begin);
                    serializedData = MemBuffer.ToArray();
                }
            }
            catch (Exception Error)
            {
                Console.WriteLine($"OnSerializingMethod:{Error}");
            }
        }

        public object GetEmpyrionObject()
        {
            if (serializedData == null) return null;

            try
            {
                using (var MemBuffer = new MemoryStream(serializedData))
                {
                    if (!EleonModdingTypes.TryGetValue(serializedDataType, out Type EleonType))
                    {
                        Console.WriteLine($"GetEmpyrionObject:?:{serializedDataType}");
                        return null;
                    }

                    Type TypedProtoBufCall = typeof(ProtoBufCall<>);
                    TypedProtoBufCall = TypedProtoBufCall.MakeGenericType(new[] { EleonType });

                    object ProtoBufCallInstance = Activator.CreateInstance(TypedProtoBufCall);
                    MethodInfo MI = TypedProtoBufCall.GetMethod("Deserialize");
                    return MI.Invoke(ProtoBufCallInstance, new[] { MemBuffer });
                }
            }
            catch (Exception Error)
            {
                Console.WriteLine($"OnDeserializedMethod:{Error}");
                return null;
            }
        }
    }
}