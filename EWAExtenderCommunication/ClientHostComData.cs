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
        ProcessInformation
    }

    [Serializable]
    public class ProcessInformation
    {
        public int Id { get; set; }
        public string CurrentDirecrory { get; set; }
    }


    [Serializable]
    public class ClientHostComData
    {
        public ClientHostCommand Command { get; set; }
        public object Data { get; set; }
    }

}
