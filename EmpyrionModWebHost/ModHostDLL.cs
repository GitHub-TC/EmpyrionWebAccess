using Eleon.Modding;
using EWAExtenderCommunication;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EmpyrionModWebHost
{
    public class ModHostDLL : ModGameAPI
    {
        private bool mExposeShutdownHost;

        public ClientMessagePipe ToEmpyrion { get; private set; }
        public ServerMessagePipe FromEmpyrion { get; private set; }
        public Dictionary<Type, Action<object>> InServerMessageHandler { get; }
        public IEnumerable<IEWAPlugin> Plugins { get; set; }

        public ILogger<ModHostDLL> Logger { get; set; }

        static DateTime mStartTime = DateTime.Now;

        public ModHostDLL([FromServices] IEnumerable<IEWAPlugin> aPlugins, ILogger<ModHostDLL> aLogger)
        {
            Logger  = aLogger;
            Plugins = aPlugins;

            Parallel.ForEach(Plugins.OfType<IDatabaseConnect>(), P => SaveApiCall(() => P.CreateAndUpdateDatabase(), P, "CreateAndUpdateDatabase"));

            InServerMessageHandler = new Dictionary<Type, Action<object>> {
                { typeof(EmpyrionGameEventData), M => HandleGameEvent               ((EmpyrionGameEventData)M) },
                { typeof(ClientHostComData    ), M => HandleClientHostCommunication ((ClientHostComData)M) }
            };

            Program.AppLifetime.AppLifetime.ApplicationStopping.Register(OnStopping);
        }

        private void OnStopping()
        {
            ToEmpyrion?.Close();
            FromEmpyrion?.Close();
        }

        public void InitComunicationChannels()
        {
            ToEmpyrion   = new ClientMessagePipe(CommandLineOptions.GetOption("-ModToEmpyrionPipe", "EWAToEmpyrionPipe")) { log = LogOut };
            FromEmpyrion = new ServerMessagePipe(CommandLineOptions.GetOption("-EmpyrionToModPipe", "EmpyrionToEWAPipe")) { log = LogOut };

            FromEmpyrion.Callback = Msg => { if (InServerMessageHandler.TryGetValue(Msg.GetType(), out Action<object> Handler)) Handler(Msg); };

            Parallel.ForEach(Plugins.OfType<IClientHostCommunication>() , P => SaveApiCall(() => P.ToEmpyrion = ToEmpyrion, P, "ToEmpyrion"));
            Parallel.ForEach(Plugins, P => SaveApiCall(() => P.Game_Start(this), P, "Game_Start"));
        }

        private void LogOut(string aMsg)
        {
            Console_Write(aMsg);
        }

        private void HandleClientHostCommunication(ClientHostComData aMsg)
        {
            //Console.WriteLine($"{aMsg.Command} = {aMsg.Data}");
            switch (aMsg.Command)
            {
                default: Parallel.ForEach(Plugins.OfType<IClientHostCommunication>(), P => SaveApiCall(() => P.ClientHostMessage(aMsg), P, "ClientHostMessage")); break;
                case ClientHostCommand.Game_Exit: HandleGameExit(false); break;
                case ClientHostCommand.Game_Update: Parallel.ForEach(Plugins, P => SaveApiCall(() => P.Game_Update(), P, "Game_Update")); break;
            }
        }

        public void HandleGameExit(bool aForce)
        {
            if (!aForce && DateTime.Now - mStartTime < new TimeSpan(0, 0, 30)) return; // Ignore Gamestop in the first 30 seconds

            Parallel.ForEach(Plugins, P => SaveApiCall(() => P.Game_Exit(), P, "Game_Exit"));
            if (aForce || !mExposeShutdownHost)
            {
                Logger.LogInformation("Game_Exit: called");
                Thread.Sleep(1000);
                Program.Application.StopAsync().Wait(30000);
            }
            mExposeShutdownHost = false;
        }

        private void HandleGameEvent(EmpyrionGameEventData aMsg)
        {
            var msg = aMsg.GetEmpyrionObject();
            Parallel.ForEach(Plugins, P => SaveApiCall(() => P.Game_Event(aMsg.eventId, aMsg.seqNr, msg), P, $"CmdId:{aMsg.eventId} seqNr:{aMsg.seqNr} data:{msg}"));
        }

        public void Console_Write(string aMsg)
        {
            //Console.WriteLine($"Console_Write:{aMsg}");
            ToEmpyrion?.SendMessage(new ClientHostComData() { Command = ClientHostCommand.Console_Write, Data = aMsg });
        }

        public ulong Game_GetTickTime()
        {
            return (ulong)DateTime.Now.Ticks;
        }

        public bool Game_Request(CmdId reqId, ushort seqNr, object data)
        {
            //Console.WriteLine($"Game_Request:{reqId}#{seqNr} = {data}");
            var msg = new EmpyrionGameEventData() { eventId = reqId, seqNr = seqNr };
            msg.SetEmpyrionObject(data);
            ToEmpyrion?.SendMessage(msg);
            return true;
        }

        public void SaveApiCall(Action aCall, object aMod, string aErrorInfo)
        {
            try
            {
                aCall();
            }
            catch (Exception Error)
            {
                Logger.LogError(Error, $"Exception [{aMod}] {aErrorInfo} => {Error}");
                LogOut($"Exception [{aMod}] {aErrorInfo} => {Error}");
            }
        }

        public void ExposeShutdownHost()
        {
            mExposeShutdownHost = true;
            Program.Host.ToEmpyrion.SendMessage(new ClientHostComData() { Command = ClientHostCommand.ExposeShutdownHost });
        }
    }

}
