using Eleon.Modding;
using EWAExtenderCommunication;
using System;
using System.Collections.Generic;
using Microsoft.Composition;
using System.Composition;
using Microsoft.AspNetCore.Mvc;
using EmpyrionModWebHost.Controllers;

namespace EmpyrionModWebHost
{
    [Export]
    public class ModHostDLL : ModGameAPI
    {
        public ClientMessagePipe ToEmpyrion { get; private set; }
        public ServerMessagePipe FromEmpyrion { get; private set; }
        public Dictionary<Type, Action<object>> InServerMessageHandler { get; }
        [ImportMany]
        public IEnumerable<IEWAPlugin> Plugins { get; set; }

        public ModHostDLL([FromServices] IEnumerable<IEWAPlugin> aPlugins)
        {
            Plugins = aPlugins;

            InServerMessageHandler = new Dictionary<Type, Action<object>> {
                { typeof(EmpyrionGameEventData), M => HandleGameEvent               ((EmpyrionGameEventData)M) },
                { typeof(ClientHostComData    ), M => HandleClientHostCommunication ((ClientHostComData)M) }
            };
        }

        public void InitComunicationChannels()
        {
            ToEmpyrion   = new ClientMessagePipe(CommandLineOptions.GetOption("-ModToEmpyrionPipe", "EWAToEmpyrionPipe")) { log = Console.WriteLine };
            FromEmpyrion = new ServerMessagePipe(CommandLineOptions.GetOption("-EmpyrionToModPipe", "EmpyrionToEWAPipe")) { log = Console.WriteLine };

            FromEmpyrion.Callback = Msg => { if (InServerMessageHandler.TryGetValue(Msg.GetType(), out Action<object> Handler)) Handler(Msg); };

            Plugins.ForEach(P => P.Game_Start(this));
        }

        private void HandleClientHostCommunication(ClientHostComData aMsg)
        {
            //Console.WriteLine($"{aMsg.Command} = {aMsg.Data}");
            switch (aMsg.Command)
            {
                case ClientHostCommand.Game_Exit  : Plugins.ForEach(P => P.Game_Exit());   break;
                case ClientHostCommand.Game_Update: Plugins.ForEach(P => P.Game_Update()); break;
            }
        }

        private void HandleGameEvent(EmpyrionGameEventData aMsg)
        {
            Console.WriteLine($"Game_Event:{aMsg.eventId}#{aMsg.seqNr} = {aMsg.serializedDataType}");

            Plugins.ForEach(P => P.Game_Event(aMsg.eventId, aMsg.seqNr, aMsg.GetEmpyrionObject()));
        }

        public void Console_Write(string aMsg)
        {
            Console.WriteLine($"Console_Write:{aMsg}");
            ToEmpyrion.SendMessage(new ClientHostComData() { Command = ClientHostCommand.Console_Write, Data = aMsg });
        }

        public ulong Game_GetTickTime()
        {
            return (ulong)DateTime.Now.Ticks;
        }

        public bool Game_Request(CmdId reqId, ushort seqNr, object data)
        {
            Console.WriteLine($"Game_Request:{reqId}#{seqNr} = {data}");
            var msg = new EmpyrionGameEventData() { eventId = reqId, seqNr = seqNr};
            msg.SetEmpyrionObject(data);
            ToEmpyrion.SendMessage(msg);
            return true;
        }
    }
}
