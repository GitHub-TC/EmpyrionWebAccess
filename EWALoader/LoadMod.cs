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
    public class LoadMod : ModInterface
    {
        string mDllNamesFileName { get; set; }
        ModGameAPI mGameAPI { get; set; }
        string[] mAssemblyFileNames { get; set; }
        List<ModInterface> mModInstance { get; set; } = new List<ModInterface>();
        ModInterface mSingleModInstance;

        public void Game_Event(CmdId eventId, ushort seqNr, object data)
        {
            try
            {
                if (mSingleModInstance != null) mSingleModInstance.Game_Event(eventId, seqNr, data);
                else mModInstance.ForEach(M => ThreadPool.QueueUserWorkItem(SubM => { ((ModInterface)SubM).Game_Event(eventId, seqNr, data); }));
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
                if (mSingleModInstance != null) mSingleModInstance.Game_Exit();
                else mModInstance.ForEach(M => ThreadPool.QueueUserWorkItem(SubM => { ((ModInterface)SubM).Game_Exit(); }));
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
                mGameAPI.Console_Write($"LoadMod(start): {mDllNamesFileName}");

                mAssemblyFileNames = File.ReadAllLines(mDllNamesFileName)
                    .Select(L => L.Trim())
                    .Where(L => !string.IsNullOrEmpty(L) && !L.StartsWith("#"))
                    .ToArray();

                Array.ForEach(mAssemblyFileNames, LoadAssembly);

                if (mModInstance.Count == 1) mSingleModInstance = mModInstance.First();

                mGameAPI.Console_Write($"LoadMod(finish:{mModInstance.Count}): {mDllNamesFileName}");
            }
            catch (Exception Error)
            {
                mGameAPI.Console_Write($"LoadMod: {mDllNamesFileName} -> {Error}");
            }
        }

        private void LoadAssembly(string aFileName)
        {
            try
            {
                var Mod = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), aFileName));
                var ModType = Mod.GetTypes().Where(T => T.GetInterfaces().Contains(typeof(ModInterface))).FirstOrDefault();
                if (ModType != null)
                {
                    var ModInstance = Activator.CreateInstance(ModType) as ModInterface;
                    ModInstance?.Game_Start(mGameAPI);

                    mModInstance.Add(ModInstance);
                }
            }
            catch (Exception Error)
            {
                mGameAPI.Console_Write($"LoadMod: {aFileName} CurrentDit:{Directory.GetCurrentDirectory()} -> {Error}");
            }
        }

        public void Game_Update()
        {
            try
            {
                if (mSingleModInstance != null) mSingleModInstance.Game_Update();
                else mModInstance.ForEach(M => ThreadPool.QueueUserWorkItem(SubM => { ((ModInterface)SubM).Game_Update(); }));
            }
            catch (Exception Error)
            {
                mGameAPI.Console_Write($"Game_Update(Error): {Error}");
            }
        }
    }
}
