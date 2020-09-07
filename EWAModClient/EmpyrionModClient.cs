using Eleon.Modding;
using EWAExtenderCommunication;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace EWAModClient
{
    public class Configuration
    {
        public string PathToModHost { get; set; } = @"..\EWA\EmpyrionModWebHost.exe";
        public string AdditionalArguments { get; set; }
        public bool AutostartModHost { get; set; } = true;
        public int AutostartModHostAfterNSeconds { get; set; } = 10;
        public bool AutoshutdownModHost { get; set; } = true;
        public string EmpyrionToModPipeName { get; set; } = "EmpyrionToEWAPipe{0}";
        public string ModToEmpyrionPipeName { get; set; } = "EWAToEmpyrionPipe{0}";
        public int HostProcessId { get; set; }
        public bool WithShellWindow { get; set; } = true;
        public int GlobalStructureListUpdateIntervallInSeconds { get; set; } = 30;
    }

    public class EmpyrionModClient : ModInterface
    {
        public ModGameAPI GameAPI { get; private set; }
        public ClientMessagePipe OutServer { get; private set; }
        public ServerMessagePipe InServer { get; private set; }
        public Process HostProcess { get; private set; }
        public DateTime? HostProcessAlive { get; private set; }
        public static string ProgramPath { get; private set; } = GetDirWith(Directory.GetCurrentDirectory(), "BuildNumber.txt");
        public bool Exit { get; private set; }
        public bool ExposeShutdownHost { get; private set; }
        public bool WithinUpdate { get; private set; }

        Dictionary<Type, Action<object>> InServerMessageHandler;

        ConfigurationManager<Configuration> CurrentConfig;
        public AutoResetEvent GetGlobalStructureList { get; set; } = new AutoResetEvent(false);
        public ConcurrentQueue<EmpyrionGameEventData> GetGlobalStructureListEvents { get; set; } = new ConcurrentQueue<EmpyrionGameEventData>();

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

            if (!ExposeShutdownHost && CurrentConfig.Current.AutoshutdownModHost && HostProcess != null)
            {
                try
                {
                    try { HostProcess.CloseMainWindow(); } catch { }
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

            GameAPI.Console_Write($"ModClientDll (CurrentDir:{Directory.GetCurrentDirectory()} Root:{ProgramPath}): Config:{CurrentConfig.ConfigFilename}");

            InServerMessageHandler = new Dictionary<Type, Action<object>> {
                { typeof(EmpyrionGameEventData), M => HandleGameEvent               ((EmpyrionGameEventData)M) },
                { typeof(ClientHostComData    ), M => HandleClientHostCommunication ((ClientHostComData)M) }
            };

            OutServer = new ClientMessagePipe(CurrentConfig.Current.EmpyrionToModPipeName) { Log = GameAPI.Console_Write };
            InServer = new ServerMessagePipe(CurrentConfig.Current.ModToEmpyrionPipeName) { Log = GameAPI.Console_Write };
            InServer.Callback = Msg =>
            {
                if (InServerMessageHandler.TryGetValue(Msg.GetType(), out Action<object> Handler)) Handler(Msg);
            };

            new Thread(() => { while (!Exit) { Thread.Sleep(1000); CheckHostProcess(); } }) { IsBackground = true }.Start();
            new Thread(() => ReadGlobalStructureInfoForEvent())                             { IsBackground = true }.Start();

            GameAPI.Console_Write($"ModClientDll: started");
        }

        private void ReadGlobalStructureInfoForEvent()
        {
            var gsl = new EgsDbTools.GlobalStructureListAccess();
            while (!Exit)
            {
                if (GetGlobalStructureList.WaitOne(1000))
                {
                    if (GetGlobalStructureListEvents.TryDequeue(out var TypedMsg))
                    {
                        gsl.UpdateIntervallInSeconds = CurrentConfig.Current.GlobalStructureListUpdateIntervallInSeconds;
                        gsl.GlobalDbPath = Path.Combine(EmpyrionConfiguration.SaveGamePath, "global.db");

                        switch (TypedMsg.eventId)
                        {
                            case CmdId.Request_GlobalStructure_List  :                       Game_Event(TypedMsg.eventId, TypedMsg.seqNr, gsl.CurrentList); break;
                            case CmdId.Request_GlobalStructure_Update: gsl.UpdateNow = true; Game_Event(TypedMsg.eventId, TypedMsg.seqNr, true);            break;
                        }
                    }
                    GetGlobalStructureList.Reset();
                }
            }
        }

        private void StartHostProcess()
        {
            var HostFilename = Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), CurrentConfig.Current.PathToModHost);
            GameAPI.Console_Write($"ModClientDll: start host '{HostFilename}'");
            HostProcessAlive = null;

            if (CurrentConfig.Current.HostProcessId != 0)
            {
                try
                {
                    HostProcess = Process.GetProcessById(CurrentConfig.Current.HostProcessId);
                    if (HostProcess.MainWindowTitle != HostFilename) HostProcess = null;
                }
                catch (Exception)
                {
                    HostProcess = null;
                }
            }

            if (CurrentConfig.Current.AutostartModHost && !string.IsNullOrEmpty(CurrentConfig.Current.PathToModHost) && ExistsStartFile())
            {
                UpdateEWA(new ProcessInformation() { Id = HostProcess == null ? 0 : HostProcess.Id });
                if(HostProcess == null) CreateHostProcess(HostFilename);
            }
        }

        private bool ExistsStartFile()
        {
            return File.Exists(Path.Combine(EmpyrionConfiguration.SaveGameModPath, "start.txt"));
        }

        private void CreateHostProcess(string HostFilename)
        {
            ProcessStartInfo StartInfo = null;
            try
            {
                StartInfo = new ProcessStartInfo(HostFilename)
                {
                    UseShellExecute = CurrentConfig.Current.WithShellWindow,
                    LoadUserProfile = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetDirectoryName(HostFilename),
                    Arguments = string.Join(" ",
                        $"-EmpyrionToModPipe {CurrentConfig.Current.EmpyrionToModPipeName}",
                        $"-ModToEmpyrionPipe {CurrentConfig.Current.ModToEmpyrionPipeName}",
                        $"-GameDir \"{ProgramPath}\"",
                        Environment.GetCommandLineArgs()?.Aggregate(string.Empty, HandleQuoteWhenNotSwitchOrContainsQuote),
                        CurrentConfig.Current.AdditionalArguments ?? string.Empty),
                };
            }
            catch (Exception error)
            {
                GameAPI.Console_Write($"ModClientDll: ProcessStartInfo:\nProgramPath: {ProgramPath}\nArgs: {Environment.GetCommandLineArgs().Aggregate(string.Empty, (S, A) => S + " " + A )}\nError: '{error}'");
                HostProcess = null;

                return;
            }

            try
            {
                HostProcess = new Process{ StartInfo = StartInfo };
                HostProcess.Start();
                CurrentConfig.Current.HostProcessId = HostProcess.Id;
                CurrentConfig.Save();
                GameAPI.Console_Write($"ModClientDll: host started '{HostFilename}/{HostProcess.Id}' -> StartInfo:'{StartInfo.FileName}' in '{StartInfo.WorkingDirectory}' with '{StartInfo.Arguments}'");
            }
            catch (Exception Error)
            {
                GameAPI.Console_Write($"ModClientDll: host start error '{HostFilename} -> {Error}' -> StartInfo:'{StartInfo.FileName}' in '{StartInfo.WorkingDirectory}' with '{StartInfo.Arguments}'");
                HostProcess = null;
            }
        }

        public static string GetDirWith(string aTestDir, string aTestFile)
        {
            return File.Exists(Path.Combine(aTestDir, aTestFile))
                ? aTestDir
                : GetDirWith(Path.GetDirectoryName(aTestDir), aTestFile);
        }

        private void CreateConfigFile(string aEWAConfigFile)
        {
            ReadNetworkInfos(out string LocalIp, out string Domain, out string Host);

            Directory.CreateDirectory(Path.GetDirectoryName(aEWAConfigFile));
            Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(aEWAConfigFile), "DB"));

            var Secret = new int[5].Aggregate("", (S, I) => S + Guid.NewGuid().ToString("D"));

            File.WriteAllText(aEWAConfigFile,
                File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "appsettings.json"))
                .Replace("{Secret}", Secret)
                .Replace("{LocalIp}", LocalIp)
                .Replace("{Domain}", Domain)
                .Replace("{Host}", Host)
                .Replace("{ComputerName}", string.IsNullOrEmpty(Host) ? "" : (Host + (string.IsNullOrEmpty(Domain) ? "" : Domain + ".")))
                );

            File.WriteAllText(Path.Combine(Path.GetDirectoryName(aEWAConfigFile), "xstart.txt"),"To start the EWA rename this File to 'start.txt'");
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
            if (Exit || WithinUpdate) return;

            if (!ExistsStartFile())
            {
                try
                {
                    if (HostProcess != null && !HostProcess.HasExited)
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

            try{ OutServer?.SendMessage(new ClientHostComData() { Command = ClientHostCommand.ProcessInformation }); } catch{}

            if (CurrentConfig.Current.AutostartModHostAfterNSeconds == 0 || !CurrentConfig.Current.AutostartModHost) return;
            try { if (HostProcess != null && !HostProcess.HasExited) return; } catch { }

            if (!HostProcessAlive.HasValue) HostProcessAlive = DateTime.Now;
            if ((DateTime.Now - HostProcessAlive.Value).TotalSeconds <= CurrentConfig.Current.AutostartModHostAfterNSeconds) return;

            HostProcessAlive = null;

            CheckForBinCopyFile();
            StartHostProcess();
        }

        private void CheckForBinCopyFile()
        {
            if (string.IsNullOrEmpty(CurrentConfig.Current.PathToModHost)) return;

            var BinHostFilename = Path.Combine(Path.Combine(
                Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), 
                Path.GetDirectoryName(CurrentConfig.Current.PathToModHost)),
                Path.GetFileNameWithoutExtension(CurrentConfig.Current.PathToModHost) + ".bin"
                );

            var HostFilename = Path.Combine(
                Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), 
                CurrentConfig.Current.PathToModHost);

            if (!File.Exists(BinHostFilename)) return;

            try{ File.Delete(HostFilename); }
            catch (Exception Error){ GameAPI.Console_Write($"CheckForBinCopyFile: delete {HostFilename} => {Error}"); }

            try { File.Move(BinHostFilename, HostFilename); }
            catch (Exception Error) { GameAPI.Console_Write($"CheckForBinCopyFile: move {BinHostFilename} -> {HostFilename} => {Error}"); }
        }

        private void HandleClientHostCommunication(ClientHostComData aMsg)
        {
            switch (aMsg.Command)
            {
                case ClientHostCommand.RestartHost          : break;
                case ClientHostCommand.ExposeShutdownHost   : ExposeShutdownHost = true; break;
                case ClientHostCommand.Console_Write        : GameAPI.Console_Write(aMsg.Data as string); break;
                case ClientHostCommand.ProcessInformation   : if (aMsg.Data == null) ReturnProcessInformation();
                                                              else RetrieveHostProcessInformation(aMsg.Data as ProcessInformation);
                                                              break;
                case ClientHostCommand.UpdateEWA            : new Thread(() => UpdateEWA(aMsg.Data as ProcessInformation)).Start(); break;
            }
        }

        private void UpdateEWA(ProcessInformation aProcessInformation)
        {
            if (!Directory.Exists(Path.Combine(EmpyrionConfiguration.ModPath, @"EWALoader\Update\EWALoader\EWA"))) return;

            try
            {
                WithinUpdate = true;
                RetrieveHostProcessInformation(aProcessInformation);

                GameAPI.Console_Write($"ModClientDll: EWA_Update");
                OutServer?.SendMessage(new ClientHostComData() { Command = ClientHostCommand.UpdateEWA });

                if(HostProcess != null)
                {
                    try
                    {
                        HostProcessAlive = null;

                        for (int i = 0; i < 5 * 60; i++)
                        {
                            Thread.Sleep(1000);
                            if (HostProcess.HasExited) break;
                        }

                        try { HostProcess.CloseMainWindow(); } catch { }

                        HostProcess = null;
                        CurrentConfig.Current.HostProcessId = 0;
                        CurrentConfig.Save();
                    }
                    catch (Exception Error)
                    {
                        GameAPI.Console_Write($"UpdateEWA: Game_Exit {Error}");
                    }
                }

                UpdateEWAFiles();
            }
            catch (Exception Error)
            {
                GameAPI.Console_Write($"UpdateEWA: EWA_Update {Error}");
            }
            finally
            {
                WithinUpdate = false;
            }
        }

        private void UpdateEWAFiles()
        {
            try
            {
                if (Directory.Exists(Path.Combine(EmpyrionConfiguration.ModPath, @"EWALoader\EWA.bak")))
                {
                    Directory.Delete(Path.Combine(EmpyrionConfiguration.ModPath, @"EWALoader\EWA.bak"), true);
                }

                Directory.Move(
                    Path.Combine(EmpyrionConfiguration.ModPath, @"EWALoader\EWA"),
                    Path.Combine(EmpyrionConfiguration.ModPath, @"EWALoader\EWA.bak")
                    );

                Directory.Move(
                    Path.Combine(EmpyrionConfiguration.ModPath, @"EWALoader\Update\EWALoader\EWA"),
                    Path.Combine(EmpyrionConfiguration.ModPath, @"EWALoader\EWA")
                    );
            }
            catch (Exception Error)
            {
                GameAPI.Console_Write($"UpdateEWA: Update {Error}");
            }
        }

        private void RetrieveHostProcessInformation(ProcessInformation aData)
        {
            if (aData == null) return;

            if(CurrentConfig.Current.HostProcessId != aData.Id) GameAPI.Console_Write($"HostProcessId: " + aData.Id);
            CurrentConfig.Current.HostProcessId = aData.Id;
            try{ HostProcess = Process.GetProcessById(CurrentConfig.Current.HostProcessId); } catch (Exception) {}
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
                    FileName         = "EmpyrionDedicated.exe",
                    Arguments        = Environment.GetCommandLineArgs().Aggregate(string.Empty, HandleQuoteWhenNotSwitchOrContainsQuote),
                }
            });
        }

        private string HandleQuoteWhenNotSwitchOrContainsQuote(string S, string A) => 
            string.Format("{0} {1}", S, !(Equals(A.FirstOrDefault(), '-') || A.Contains('"')) ? $"\"{A}\"" : A).Trim();        

        private void HandleGameEvent(EmpyrionGameEventData TypedMsg)
        {
            if(TypedMsg.eventId == CmdId.Request_GlobalStructure_List || TypedMsg.eventId == CmdId.Request_GlobalStructure_Update)
            {
                GetGlobalStructureListEvents.Enqueue(TypedMsg);
                GetGlobalStructureList.Set();
            }
            else GameAPI.Game_Request(TypedMsg.eventId, TypedMsg.seqNr, TypedMsg.GetEmpyrionObject());
        }

        public void Game_Update()
        {
            if (Exit) return;
            OutServer?.SendMessage(new ClientHostComData() { Command = ClientHostCommand.Game_Update });
        }
    }
}