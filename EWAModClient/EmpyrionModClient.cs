using Eleon.Modding;
using EWAExtenderCommunication;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace EWAModClient
{
    public class Configuration
    {
    	public string StartWith { get; set; }
        public string PathToModHost { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"..\EWA\EmpyrionModWebHost.exe"
            : @"../EWA-linux/EmpyrionModWebHost";
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

    public class EmpyrionModClient : ModInterface, IMod
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
        public IModApi ModAPI { get; private set; }
        public ulong GameTicks { get; private set; }

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
                    try {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            HostProcess.CloseMainWindow();
                        else
                            HostProcess.Kill();
                    } catch { }
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

            OutServer = new ClientMessagePipe(CurrentConfig.Current.EmpyrionToModPipeName) { Log = GameAPI.Console_Write, UseTcp = IsWineEnvironment() };
            InServer  = new ServerMessagePipe(CurrentConfig.Current.ModToEmpyrionPipeName) { Log = GameAPI.Console_Write, UseTcp = IsWineEnvironment() };
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
                try
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
                    }
                }
                catch (Exception error)
                {
                    GameAPI.Console_Write($"ReadGlobalStructureInfoForEvent:{error}");
                }
                finally
                {
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
                var useShell = CurrentConfig.Current.WithShellWindow && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !IsWineEnvironment();
                StartInfo = BuildProcessStartInfo(HostFilename, useShell);
            }
            catch (Exception error)
            {
                GameAPI.Console_Write($"ModClientDll: ProcessStartInfo:\nProgramPath: {ProgramPath}\nArgs: {Environment.GetCommandLineArgs().Aggregate(string.Empty, (S, A) => S + " " + A )}\nError: '{error}'");
                HostProcess = null;

                return;
            }

            try
            {
                var hostDir   = Path.GetDirectoryName(HostFilename);
                var scriptLog = Path.Combine(hostDir, "EmpyrionModWebHost.sh.log");
                var hostLog   = Path.Combine(hostDir, "EmpyrionModWebHost.log");
                TryDeleteFile(scriptLog);
                TryDeleteFile(hostLog);

                HostProcess = new Process{ StartInfo = StartInfo };
                // Do NOT set EnableRaisingEvents=true — DuplicateHandle fails under Wine spawning native Linux ELF
                HostProcess.Start();
                CurrentConfig.Current.HostProcessId = HostProcess.Id;
                CurrentConfig.Save();
                GameAPI.Console_Write($"ModClientDll: host started '{HostFilename}/{HostProcess.Id}' -> StartInfo:'{StartInfo.FileName}' in '{StartInfo.WorkingDirectory}' with '{StartInfo.Arguments}'");

                new Thread(() => {
                    Thread.Sleep(3000);
                    try
                    {
                        if (File.Exists(scriptLog))
                            foreach (var line in File.ReadAllLines(scriptLog))
                                GameAPI.Console_Write($"[sh] {line}");
                        if (File.Exists(hostLog))
                            foreach (var line in File.ReadAllLines(hostLog))
                                GameAPI.Console_Write($"[host] {line}");
                    }
                    catch (Exception ex) { GameAPI.Console_Write($"[host] log read error: {ex.Message}"); }
                }) { IsBackground = true }.Start();
            }
            catch (Exception Error)
            {
                GameAPI.Console_Write($"ModClientDll: host start error '{HostFilename} -> {Error}' -> StartInfo:'{StartInfo.FileName}' in '{StartInfo.WorkingDirectory}' with '{StartInfo.Arguments}'");
                HostProcess = null;
            }
        }

        private ProcessStartInfo BuildProcessStartInfo(string hostFilename, bool useShell)
        {
            string executable;
            string hostArg;
            bool wineMode = IsWineEnvironment();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !wineMode)
            {
                executable = string.IsNullOrEmpty(CurrentConfig.Current.StartWith) ? hostFilename : CurrentConfig.Current.StartWith;
                hostArg    = string.IsNullOrEmpty(CurrentConfig.Current.StartWith) ? "" : $"\"{CurrentConfig.Current.PathToModHost}\"";
            }
            else if (wineMode)
            {
                // Wine on Linux: spawn native Linux ELF binary via bash shell script
                var hostDir    = Path.GetDirectoryName(hostFilename);
                var binaryName = Path.GetFileNameWithoutExtension(hostFilename); // strip .exe

                // Prefer a sibling "-linux" folder (e.g. EWA-linux\ next to EWA\)
                var linuxDir          = hostDir + "-linux";
                var linuxBinaryAlt    = Path.Combine(linuxDir, binaryName);
                var linuxBinary       = File.Exists(linuxBinaryAlt)
                    ? linuxBinaryAlt
                    : Path.Combine(hostDir, binaryName);

                WineChmodExecutable(linuxBinary);

                var bash = FindNativeLinuxTool("/bin/bash", "/usr/bin/bash");
                if (bash != null)
                {
                    var scriptPath = GenerateWineHostScript(hostFilename, linuxBinary);
                    executable = bash;
                    hostArg    = $"\"{WinePathToUnix(scriptPath)}\"";
                    useShell   = false;
                    GameAPI.Console_Write($"Wine-Umgebung: starte EWA Host via '{WinePathToUnix(scriptPath)}'");
                }
                else
                {
                    executable = linuxBinary;
                    hostArg    = "";
                    useShell   = false;
                    GameAPI.Console_Write($"Wine-Umgebung: starte Linux-Binary '{WinePathToUnix(linuxBinary)}'");
                }
            }
            else
            {
                var dllPath = Path.ChangeExtension(hostFilename, ".dll");
                if (File.Exists(dllPath))
                {
                    executable = "dotnet";
                    hostArg    = $"\"{dllPath}\"";
                }
                else
                {
                    EnsureExecutable(hostFilename);
                    executable = hostFilename;
                    hostArg    = "";
                }
            }

            var wineFlag = wineMode ? "-WineMode" : "";

            var arguments = string.Join(" ", new[] {
                hostArg,
                $"-EmpyrionToModPipe {CurrentConfig.Current.EmpyrionToModPipeName}",
                $"-ModToEmpyrionPipe {CurrentConfig.Current.ModToEmpyrionPipeName}",
                wineFlag,
                $"-GameDir \"{(wineMode ? WinePathToUnix(ProgramPath) : ProgramPath)}\"",
                Environment.GetCommandLineArgs()?.Aggregate(string.Empty, HandleQuoteWhenNotSwitchOrContainsQuote),
                CurrentConfig.Current.AdditionalArguments ?? string.Empty
            }.Where(s => !string.IsNullOrEmpty(s))).Trim();

            return new ProcessStartInfo(executable)
            {
                UseShellExecute  = useShell,
                LoadUserProfile  = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !wineMode,
                CreateNoWindow   = true,
                WindowStyle      = ProcessWindowStyle.Hidden,
                WorkingDirectory = string.IsNullOrEmpty(CurrentConfig.Current.StartWith)
                                   ? Path.GetDirectoryName(hostFilename)
                                   : ProgramPath,
                Arguments        = arguments,
            };
        }

        private static void EnsureExecutable(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !File.Exists(path)) return;
            try
            {
                var chmod = new Process
                {
                    StartInfo = new ProcessStartInfo("chmod", $"+x \"{path}\"")
                    {
                        UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
                    }
                };
                chmod.Start();
                chmod.WaitForExit(5000);
            }
            catch (Exception ex) { Console.WriteLine($"EnsureExecutable: {ex.Message}"); }
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

            bool hostAlive;
            if (IsWineEnvironment())
                hostAlive = InServer?.Connected == true;
            else
                hostAlive = HostProcess != null && GetHasExited(HostProcess) == false;

            if (hostAlive) { HostProcessAlive = null; return; }

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
                case ClientHostCommand.GameTicks            : if (aMsg.Data == null) ReturnGameTicks(); 
                                                              else GameTicks = (ulong) aMsg.Data;
                                                              break;
            }
        }

        private void UpdateEWA(ProcessInformation aProcessInformation)
        {
            if (!Directory.Exists(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Update", "EWALoader", "EWA"))) return;

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

                        try {
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                HostProcess.CloseMainWindow();
                            else
                                HostProcess.Kill();
                        } catch { }

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
                if (Directory.Exists(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "EWA.bak"      ))) Directory.Delete(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "EWA.bak"), true);
                if (Directory.Exists(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "EWA-linux.bak"))) Directory.Delete(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "EWA-linux.bak"), true);

                Directory.Move(
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "EWA"),
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "EWA.bak")
                    );

                Directory.Move(
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "EWA-linux"),
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "EWA-linux.bak")
                    );

                Directory.Move(
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Update", "EWALoader", "EWA"),
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "EWA")
                    );

                Directory.Move(
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Update", "EWALoader", "EWA-linux"),
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "EWA-linux")
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

        private void ReturnGameTicks()
        {
            OutServer.SendMessage(new ClientHostComData()
            {
                Command = ClientHostCommand.GameTicks,
                Data    = GameAPI.Game_GetTickTime()
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
            else if (TypedMsg.eventId == CmdId.Event_ChatMessage + 100)
            {
                object receiveObject = null;
                try
                {
                    receiveObject = TypedMsg.GetEmpyrionObject<Eleon.MessageData>();
                    ModAPI.Application.SendChatMessage(receiveObject as Eleon.MessageData);
                    Game_Event(TypedMsg.eventId, TypedMsg.seqNr, true);
                }
                catch (Exception error)
                {
                    ModAPI.LogError($"SendChatMessage:[{receiveObject}] {error}");
                }
            }
            else GameAPI.Game_Request(TypedMsg.eventId, TypedMsg.seqNr, TypedMsg.GetEmpyrionObject());
        }

        public void Game_Update()
        {
            if (Exit) return;
            OutServer?.SendMessage(new ClientHostComData() { Command = ClientHostCommand.Game_Update });
        }

        public void Init(IModApi modAPI)
        {
            ModAPI = modAPI;
        }

        public void Shutdown()
        {
        }

        private static bool IsWineEnvironment()
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
               (File.Exists(@"Z:\bin\bash") || File.Exists(@"Z:\usr\bin\bash"));

        private static string FindNativeLinuxTool(params string[] unixPaths)
        {
            foreach (var unixPath in unixPaths)
            {
                var winePath = "Z:" + unixPath.Replace('/', '\\');
                if (File.Exists(winePath)) return winePath;
            }
            return null;
        }

        private static string WinePathToUnix(string winePath)
            => winePath.StartsWith("Z:") || winePath.StartsWith("z:")
                ? winePath.Substring(2).Replace('\\', '/')
                : winePath.Replace('\\', '/');

        private static void WineChmodExecutable(string wineExePath)
        {
            var unixPath = WinePathToUnix(wineExePath);
            if (!File.Exists(wineExePath)) return;
            var bash = FindNativeLinuxTool("/bin/bash", "/usr/bin/bash");
            if (bash == null) return;
            try
            {
                using var p = Process.Start(new ProcessStartInfo(bash, $"-c \"chmod +x '{unixPath}'\"")
                    { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true });
                p?.WaitForExit(5000);
                Console.WriteLine($"chmod +x '{unixPath}' -> exit {p?.ExitCode}");
            }
            catch (Exception ex) { Console.WriteLine($"WineChmodExecutable: {ex.Message}"); }
        }

        private static string GenerateWineHostScript(string wineExePath, string linuxBinaryWinePath)
        {
            var elfUnixPath    = WinePathToUnix(linuxBinaryWinePath);
            var scriptWinePath = Path.ChangeExtension(wineExePath, ".sh");
            var sb = new System.Text.StringBuilder();
            sb.Append("#!/bin/bash\n");
            sb.Append($"ELF=\"{elfUnixPath}\"\n");
            sb.Append("LOG=\"$(dirname \"$ELF\")/EmpyrionModWebHost.sh.log\"\n");
            sb.Append("{\n");
            sb.Append("  echo \"[$(date)] Script gestartet\"\n");
            sb.Append("  echo \"Args: $@\"\n");
            sb.Append("  echo \"uname: $(uname -a)\"\n");
            sb.Append("  echo \"ldd version: $(ldd --version 2>&1 | head -1)\"\n");
            sb.Append("  echo \"ldd EmpyrionModWebHost:\"\n");
            sb.Append("  ldd \"$ELF\" 2>&1\n");
            sb.Append("  echo \"--- Start EmpyrionModWebHost ---\"\n");
            sb.Append("  \"$ELF\" \"$@\" >> \"$LOG\" 2>&1\n");
            sb.Append("  echo \"[$(date)] Exit: $?\"\n");
            sb.Append("} >> \"$LOG\" 2>&1\n");
            File.WriteAllText(scriptWinePath, sb.ToString());
            return scriptWinePath;
        }

        private static bool GetHasExited(Process p)
        {
            try { return p.HasExited; }
            catch { return true; }
        }

        private static void TryDeleteFile(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
    }
}