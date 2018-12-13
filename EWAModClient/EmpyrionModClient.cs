using Eleon.Modding;
using EWAExtenderCommunication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;

namespace EWAModClient
{
    public class Configuration
    {
        public string PathToModHost { get; set; } = @"..\EWA\EmpyrionModWebHost.exe";
        public bool AutostartModHost { get; set; } = true;
        public int AutostartModHostAfterNSeconds { get; set; } = 10;
        public bool AutoshutdownModHost { get; set; } = true;
        public string EmpyrionToModPipeName { get; set; } = "EmpyrionToEWAPipe{0}";
        public string ModToEmpyrionPipeName { get; set; } = "EWAToEmpyrionPipe{0}";
        public int HostProcessId { get; set; }
        public bool WithShellWindow { get; set; }
    }

    public class EmpyrionModClient : ModInterface
    {
        public ModGameAPI GameAPI { get; private set; }
        public ClientMessagePipe OutServer { get; private set; }
        public ServerMessagePipe InServer { get; private set; }
        public Process mHostProcess { get; private set; }
        public DateTime? mHostProcessAlive { get; private set; }
        public static string ProgramPath { get; private set; } = Directory.GetCurrentDirectory();
        public bool Exit { get; private set; }
        public bool ExposeShutdownHost { get; private set; }

        Dictionary<Type, Action<object>> InServerMessageHandler;

        ConfigurationManager<Configuration> CurrentConfig;

        public void Game_Event(CmdId eventId, ushort seqNr, object data)
        {
            if (OutServer == null) return;

            try
            {
                var msg = new EmpyrionGameEventData() { eventId = eventId, seqNr = seqNr };
                msg.SetEmpyrionObject(data);
                OutServer.SendMessage(msg);
            }
            catch (System.Exception Error)
            {
                GameAPI.Console_Write($"ModClientDll: {Error.Message}");
            }
        }

        public void Game_Exit()
        {
            Exit = true;
            GameAPI.Console_Write($"ModClientDll: Game_Exit {CurrentConfig.Current.ModToEmpyrionPipeName}");
            OutServer?.SendMessage(new ClientHostComData() { Command = ClientHostCommand.Game_Exit });

            if (!ExposeShutdownHost && CurrentConfig.Current.AutoshutdownModHost && mHostProcess != null)
            {
                try
                {
                    try { mHostProcess.CloseMainWindow(); } catch { }
                    CurrentConfig.Current.HostProcessId = 0;
                    CurrentConfig.Save();

                    Thread.Sleep(1000);
                }
                catch (Exception Error)
                {
                    GameAPI.Console_Write($"ModClientDll: Game_Exit {Error}");
                }
            }

            InServer?.Close();
            OutServer?.Close();
        }

        public void Game_Start(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            GameAPI.Console_Write($"ModClientDll: start");

            CurrentConfig = new ConfigurationManager<Configuration>()
            {
                ConfigFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "Configuration.xml")
            };
            CurrentConfig.Load();
            CurrentConfig.Current.EmpyrionToModPipeName = string.Format(CurrentConfig.Current.EmpyrionToModPipeName, Guid.NewGuid().ToString("N"));
            CurrentConfig.Current.ModToEmpyrionPipeName = string.Format(CurrentConfig.Current.ModToEmpyrionPipeName, Guid.NewGuid().ToString("N"));
            CurrentConfig.Save();

            var EWAConfigFile = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "appsettings.json");
            if (!File.Exists(EWAConfigFile)) CreateConfigFile(EWAConfigFile);

            GameAPI.Console_Write($"ModClientDll (CurrentDir:{Directory.GetCurrentDirectory()}): Config:{CurrentConfig.ConfigFilename}");

            InServerMessageHandler = new Dictionary<Type, Action<object>> {
                { typeof(EmpyrionGameEventData), M => HandleGameEvent               ((EmpyrionGameEventData)M) },
                { typeof(ClientHostComData    ), M => HandleClientHostCommunication ((ClientHostComData)M) }
            };

            OutServer = new ClientMessagePipe(CurrentConfig.Current.EmpyrionToModPipeName) { log = GameAPI.Console_Write };
            InServer = new ServerMessagePipe(CurrentConfig.Current.ModToEmpyrionPipeName) { log = GameAPI.Console_Write };
            InServer.Callback = Msg => {
                if (InServerMessageHandler.TryGetValue(Msg.GetType(), out Action<object> Handler)) Handler(Msg);
            };

            new Thread(() => { while (!Exit) { Thread.Sleep(1000); CheckHostProcess(); } }).Start();

            GameAPI.Console_Write($"ModClientDll: started");
        }

        private void StartHostProcess()
        {
            var HostFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), CurrentConfig.Current.PathToModHost);
            GameAPI.Console_Write($"ModClientDll: start host '{HostFilename}'");
            mHostProcessAlive = null;

            if (CurrentConfig.Current.HostProcessId != 0)
            {
                try
                {
                    mHostProcess = Process.GetProcessById(CurrentConfig.Current.HostProcessId);
                    if (mHostProcess.MainWindowTitle != HostFilename) mHostProcess = null;
                }
                catch (Exception)
                {
                    mHostProcess = null;
                }
            }

            if (mHostProcess == null && CurrentConfig.Current.AutostartModHost && !string.IsNullOrEmpty(CurrentConfig.Current.PathToModHost))
            {
                if (ExistsStartFile()) CreateHostProcess(HostFilename);
            }
        }

        private bool ExistsStartFile()
        {
            return File.Exists(Path.Combine(EmpyrionConfiguration.SaveGameModPath, "start.txt"));
        }

        private void CreateHostProcess(string HostFilename)
        {
            try
            {
                mHostProcess = new Process
                {
                    StartInfo = new ProcessStartInfo(HostFilename)
                    {
                        UseShellExecute = CurrentConfig.Current.WithShellWindow,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(HostFilename),
                        Arguments = Environment.GetCommandLineArgs().Aggregate(
                            $"-EmpyrionToModPipe {CurrentConfig.Current.EmpyrionToModPipeName} " + 
                            $"-ModToEmpyrionPipe {CurrentConfig.Current.ModToEmpyrionPipeName} " +
                            $"-GameDir \"{Directory.GetCurrentDirectory()}\"",
                            (C, A) => C + " " + A),
                    }
                };

                mHostProcess.Start();
                CurrentConfig.Current.HostProcessId = mHostProcess.Id;
                CurrentConfig.Save();
                GameAPI.Console_Write($"ModClientDll: host started '{HostFilename}/{mHostProcess.Id}'");
            }
            catch (Exception Error)
            {
                GameAPI.Console_Write($"ModClientDll: host start error '{HostFilename} -> {Error}'");
                mHostProcess = null;
            }
        }

        private void CreateConfigFile(string aEWAConfigFile)
        {
            ReadNetworkInfos(out string LocalIp, out string Domain, out string Host);

            Directory.CreateDirectory(Path.GetDirectoryName(aEWAConfigFile));
            File.WriteAllText(aEWAConfigFile,
                File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "appsettings.json"))
                .Replace("{LocalIp}", LocalIp)
                .Replace("{Domain}", Domain)
                .Replace("{Host}", Host)
                .Replace("{ComputerName}", string.IsNullOrEmpty(Host) ? "" : (Host + (string.IsNullOrEmpty(Domain) ? "" : Domain + ".")))
                );
        }

        private static void ReadNetworkInfos(out string LocalIp, out string Domain, out string Host)
        {
            LocalIp = string.Empty;
            Domain = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
            Host = System.Net.Dns.GetHostName();

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return;

            System.Net.IPHostEntry host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());

            foreach (System.Net.IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    LocalIp = ip.ToString();
                    break;
                }
            }
        }

        void CheckHostProcess()
        {
            if (Exit) return;

            if (!ExistsStartFile())
            {
                try
                {
                    if (mHostProcess != null && !mHostProcess.HasExited)
                    {
                        GameAPI.Console_Write($"ModClientDll: start.txt not found");

                        OutServer?.SendMessage(new ClientHostComData() { Command = ClientHostCommand.Game_Exit });
                        Thread.Sleep(1000);

                        GameAPI.Console_Write($"ModClientDll: stopped");
                    }
                }
                catch { }

                return;
            }

            if (CurrentConfig.Current.AutostartModHostAfterNSeconds == 0 || !CurrentConfig.Current.AutostartModHost) return;
            try { if (mHostProcess != null && !mHostProcess.HasExited) return; } catch { }

            if (!mHostProcessAlive.HasValue) mHostProcessAlive = DateTime.Now;
            if ((DateTime.Now - mHostProcessAlive.Value).TotalSeconds <= CurrentConfig.Current.AutostartModHostAfterNSeconds) return;

            mHostProcessAlive = null;

            StartHostProcess();
        }

        private void HandleClientHostCommunication(ClientHostComData aMsg)
        {
            switch (aMsg.Command)
            {
                case ClientHostCommand.RestartHost          : break;
                case ClientHostCommand.ExposeShutdownHost   : ExposeShutdownHost = true; break;
                case ClientHostCommand.Console_Write        : GameAPI.Console_Write(aMsg.Data as string); break;
                case ClientHostCommand.ProcessInformation   : if (aMsg.Data == null) ReturnProcessInformation(); break;
            }
        }

        private void ReturnProcessInformation()
        {
            OutServer.SendMessage(new ClientHostComData()
            {
                Command = ClientHostCommand.ProcessInformation,
                Data = new ProcessInformation()
                {
                    Id               = Process.GetCurrentProcess().Id,
                    CurrentDirecrory = Directory.GetCurrentDirectory(),
                    FileName         = Path.Combine(Directory.GetCurrentDirectory(), "EmpyrionDedicated.exe"),
                    Arguments        = Environment.GetCommandLineArgs().Aggregate("", (S, A) => S + " " + A),
                }
            });
        }

        private void HandleGameEvent(EmpyrionGameEventData TypedMsg)
        {
            var msg = TypedMsg.GetEmpyrionObject();
            GameAPI.Game_Request(TypedMsg.eventId, TypedMsg.seqNr, msg);
        }

        public void Game_Update()
        {
            if (Exit) return;
            OutServer?.SendMessage(new ClientHostComData() { Command = ClientHostCommand.Game_Update });
        }
    }
}