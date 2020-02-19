using Eleon.Modding;
using EWAExtenderCommunication;
using System;
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
    }

    public class EmpyrionModClient : ModInterface
    {
        public ModGameAPI GameAPI { get; private set; }
        public ClientMessagePipe OutServer { get; private set; }
        public ServerMessagePipe InServer { get; private set; }
        public Process mHostProcess { get; private set; }
        public DateTime? mHostProcessAlive { get; private set; }
        public static string ProgramPath { get; private set; } = GetDirWith(Directory.GetCurrentDirectory(), "BuildNumber.txt");
        public bool Exit { get; private set; }
        public bool ExposeShutdownHost { get; private set; }
        public bool WithinUpdate { get; private set; }

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

            GameAPI.Console_Write($"ModClientDll (CurrentDir:{Directory.GetCurrentDirectory()} Root:{ProgramPath}): Config:{CurrentConfig.ConfigFilename}");

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

            if (CurrentConfig.Current.AutostartModHost && !string.IsNullOrEmpty(CurrentConfig.Current.PathToModHost) && ExistsStartFile())
            {
                UpdateEWA(new ProcessInformation() { Id = mHostProcess == null ? 0 : mHostProcess.Id });
                if(mHostProcess == null) CreateHostProcess(HostFilename);
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
                        LoadUserProfile = true,
                        CreateNoWindow  = true,
                        WindowStyle     = ProcessWindowStyle.Hidden,
                        WorkingDirectory = Path.GetDirectoryName(HostFilename),
                        Arguments = string.Join(" ",
                            $"-EmpyrionToModPipe {CurrentConfig.Current.EmpyrionToModPipeName}",
                            $"-ModToEmpyrionPipe {CurrentConfig.Current.ModToEmpyrionPipeName}",
                            $"-GameDir \"{ProgramPath}\"",
                            Environment.GetCommandLineArgs().Aggregate(string.Empty, HandleQuoteWhenNotSwitch),
                            CurrentConfig.Current.AdditionalArguments ?? string.Empty),
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

            try{ OutServer?.SendMessage(new ClientHostComData() { Command = ClientHostCommand.ProcessInformation }); } catch{}

            if (CurrentConfig.Current.AutostartModHostAfterNSeconds == 0 || !CurrentConfig.Current.AutostartModHost) return;
            try { if (mHostProcess != null && !mHostProcess.HasExited) return; } catch { }

            if (!mHostProcessAlive.HasValue) mHostProcessAlive = DateTime.Now;
            if ((DateTime.Now - mHostProcessAlive.Value).TotalSeconds <= CurrentConfig.Current.AutostartModHostAfterNSeconds) return;

            mHostProcessAlive = null;

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

                if(mHostProcess != null)
                {
                    try
                    {
                        mHostProcessAlive = null;

                        for (int i = 0; i < 5 * 60; i++)
                        {
                            Thread.Sleep(1000);
                            if (mHostProcess.HasExited) break;
                        }

                        try { mHostProcess.CloseMainWindow(); } catch { }

                        mHostProcess = null;
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
            try{ mHostProcess = Process.GetProcessById(CurrentConfig.Current.HostProcessId); } catch (Exception) {}
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
                    Arguments        = Environment.GetCommandLineArgs().Aggregate(string.Empty, HandleQuoteWhenNotSwitch),
                }
            });
        }

        private string HandleQuoteWhenNotSwitch(string S, string A) => 
            string.Format("{0} {1}", S, !Equals(A.FirstOrDefault(), '-') ? $"\"{A}\"" : A).Trim();        

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