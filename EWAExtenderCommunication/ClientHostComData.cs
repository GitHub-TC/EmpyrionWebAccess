using System;

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

    [Serializable]
    public class ProcessInformation
    {
        public int Id { get; set; }
        public string CurrentDirecrory { get; set; }
        public string Arguments { get; set; }
        public string FileName { get; set; }
    }


    [Serializable]
    public class ClientHostComData
    {
        public ClientHostCommand Command { get; set; }
        public object Data { get; set; }
    }

}
