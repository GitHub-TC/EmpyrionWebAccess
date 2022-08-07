using Eleon.Modding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace EWALoader
{
    /// <summary>
    /// Zu ladenen Mods werden über die Datei "DllNames.txt" im Mod-Verzeichnis vom ModLoader definiert.
    /// - pro Zeile ein Pfad zu einer DLL Moddatei
    /// - Leerzeilen sind erlaubt
    /// - Kommentarzeilen beginnnen mit einen #
    /// </summary>
    public class LoadMod : ModInterface, IMod
    {
        string mDllNamesFileName { get; set; }
        ModGameAPI mGameAPI { get; set; }
        string[] mAssemblyFileNames { get; set; }
        List<ModInterface> ModInterfaces { get; set; } = new List<ModInterface>();
        List<IMod> IMods { get; set; } = new List<IMod>();
        public IModApi ModAPI { get; set; }

        ModInterface mSingleModInterfaceInstance;
        IMod mSingleIModInstance;
        private int inst;

        public void Game_Event(CmdId eventId, ushort seqNr, object data)
        {
            try
            {
                if (mSingleModInterfaceInstance != null) mSingleModInterfaceInstance.Game_Event(eventId, seqNr, data);
                else ModInterfaces.ForEach(M => ThreadPool.QueueUserWorkItem(SubM => { ((ModInterface)SubM).Game_Event(eventId, seqNr, data); }));
            }
            catch (Exception Error)
            {
                mGameAPI.Console_Write($"Game_Event(Error): {Error}");
            }
        }

        public void Game_Exit()
        {
            try
            {
                if (mSingleModInterfaceInstance != null) mSingleModInterfaceInstance.Game_Exit();
                else ModInterfaces.ForEach(M => ThreadPool.QueueUserWorkItem(SubM => { ((ModInterface)SubM).Game_Exit(); }));
            }
            catch (Exception Error)
            {
                mGameAPI.Console_Write($"Game_Exit(Error): {Error}");
            }
        }

        public void Game_Start(ModGameAPI dediAPI)
        {
            mDllNamesFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "DllNames.txt");
            mGameAPI = dediAPI;
            try
            {
                Interlocked.Increment(ref inst);
                mGameAPI.Console_Write($"LoadMod(start)[{inst}]: {mDllNamesFileName}");

                if (!File.Exists(mDllNamesFileName)) File.WriteAllText(mDllNamesFileName, @"#..\[PathToDLLFile]");

                mAssemblyFileNames = File.ReadAllLines(mDllNamesFileName)
                    .Select(L => L.Trim())
                    .Where(L => !string.IsNullOrEmpty(L) && !L.StartsWith("#"))
                    .ToArray();

                Array.ForEach(mAssemblyFileNames, LoadAssembly);

                if (ModInterfaces.Count == 1) mSingleModInterfaceInstance = ModInterfaces.First();
                if (IMods.Count == 1) mSingleIModInstance = IMods.First();

                mGameAPI.Console_Write($"LoadMod(finish:{ModInterfaces.Count}): {mDllNamesFileName}");
            }
            catch (Exception Error)
            {
                mGameAPI.Console_Write($"LoadMod: {mDllNamesFileName} -> {Error}");
            }
        }

        private void LoadAssembly(string dllFileName)
        {
            var currentDir = Directory.GetCurrentDirectory();
            try
            {
                var dllName = dllFileName;
                if (dllName.StartsWith("-"))
                {
                    dllName = dllName.Substring(1);
                    Directory.SetCurrentDirectory(Path.GetDirectoryName(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), dllName)));
                }

                var Mod = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), dllName));
                var ModType = Mod.GetTypes().Where(T => T.GetInterfaces().Contains(typeof(ModInterface))).FirstOrDefault();
                if (ModType == null)
                {
                    ModType = Mod.GetTypes().Where(T => T.GetInterfaces().Contains(typeof(IMod))).FirstOrDefault();
                }

                if (ModType != null)
                {
                    var instance = Activator.CreateInstance(ModType);

                    if (instance is ModInterface ModInterfaceInstance)
                    {
                        ModInterfaceInstance.Game_Start(mGameAPI);
                        ModInterfaces.Add(ModInterfaceInstance);
                    }

                    if (instance is IMod ModInstance)
                    {
                        IMods.Add(ModInstance);
                    }
                }
            }
            catch (Exception Error)
            {
                mGameAPI.Console_Write($"LoadMod: {dllFileName} CurrentDir:{Directory.GetCurrentDirectory()} -> {Error}");
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDir);
            }
        }

        public void Game_Update()
        {
            try
            {
                if (mSingleModInterfaceInstance != null) mSingleModInterfaceInstance.Game_Update();
                else ModInterfaces.ForEach(M => ThreadPool.QueueUserWorkItem(SubM => { ((ModInterface)SubM).Game_Update(); }));
            }
            catch (Exception Error)
            {
                mGameAPI.Console_Write($"Game_Update(Error): {Error}");
            }
        }

        public void Init(IModApi modAPI)
        {
            ModAPI = modAPI;

            Interlocked.Increment(ref inst);

            ModAPI.Log($"LoadMod(Init)[{inst}]");

            try
            {
                if (mSingleIModInstance != null) mSingleIModInstance.Init(ModAPI);
                else IMods.ForEach(M => M.Init(ModAPI));
            }
            catch (Exception error)
            {
                ModAPI.LogError($"LoadMod(Init): error : {error}");
            }
        }

        public void Shutdown()
        {
            try
            {
                if (mSingleIModInstance != null) mSingleIModInstance.Shutdown();
                else IMods.ForEach(M => ThreadPool.QueueUserWorkItem(SubM => { ((IMod)SubM).Shutdown(); }));
            }
            catch (Exception error)
            {
                ModAPI.LogError($"LoadMod(Shutdown): error : {error}");
            }
        }
    }
}
