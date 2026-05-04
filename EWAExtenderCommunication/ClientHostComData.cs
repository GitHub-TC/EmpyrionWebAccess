using System;
using ProtoBuf;

namespace EWAExtenderCommunication
{
    public enum ClientHostCommand
    {
        Game_Exit,
        RestartHost,
        Game_Update,
        Console_Write,
        ExposeShutdownHost,
        Ping,
        ProcessInformation,
        UpdateEWA,
        GameTicks
    }

    [ProtoContract]
    public class ProcessInformation
    {
        [ProtoMember(1)] public int Id { get; set; }
        [ProtoMember(2)] public string CurrentDirecrory { get; set; }
        [ProtoMember(3)] public string Arguments { get; set; }
        [ProtoMember(4)] public string FileName { get; set; }
    }

    [ProtoContract]
    public class ClientHostComData
    {
        [ProtoMember(1)]
        public ClientHostCommand Command { get; set; }

        // Typed wire-protocol backing fields (protobuf-net cannot serialize 'object' directly)
        [ProtoMember(2)] public string DataString { get; set; }
        [ProtoMember(3)] public ProcessInformation DataProcessInfo { get; set; }
        [ProtoMember(4)] public ulong DataULong { get; set; }
        [ProtoMember(5)] public bool DataULongSet { get; set; }

        // Convenience property — maps to/from the typed fields above; not serialized directly
        public object Data
        {
            get
            {
                if (DataProcessInfo != null) return DataProcessInfo;
                if (DataString      != null) return DataString;
                if (DataULongSet)            return DataULong;
                return null;
            }
            set
            {
                DataString      = value as string;
                DataProcessInfo = value as ProcessInformation;
                if (value is ulong ul) { DataULong = ul; DataULongSet = true; }
                else                   { DataULong = 0;  DataULongSet = false; }
            }
        }
    }
}
