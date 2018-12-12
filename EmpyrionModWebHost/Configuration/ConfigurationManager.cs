using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace EmpyrionModWebHost
{
    public class ConfigurationManager<T>
    {
        public string ConfigFilename {
            get => _mConfigFilename;
            set {
                _mConfigFilename = value;
                ActivateFileChangeWatcher();
            }
        }
        string _mConfigFilename;

        FileSystemWatcher mConfigFileChangedWatcher { get; set; }

        public T Current { get; set; }

        public static Action<string> Log { get; set; }

        private void ActivateFileChangeWatcher()
        {
            if (mConfigFileChangedWatcher != null) mConfigFileChangedWatcher.EnableRaisingEvents = false;
            mConfigFileChangedWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(ConfigFilename),
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = Path.GetFileName(ConfigFilename)
            };
            mConfigFileChangedWatcher.Changed += (s, e) => Load();
            mConfigFileChangedWatcher.EnableRaisingEvents = true;
        }

        public void Load()
        {
            try
            {
                Log?.Invoke($"ConfigurationManager load '{ConfigFilename}'");
                var serializer = new XmlSerializer(typeof(T));
                using (var reader = XmlReader.Create(ConfigFilename))
                {
                    Current = (T)serializer.Deserialize(reader);
                }
            }
            catch (Exception Error)
            {
                Log?.Invoke($"ConfigurationManager load '{ConfigFilename}' error {Error}");
                Current = (T)Activator.CreateInstance(typeof(T));
            }
        }

        public void Save()
        {
            try
            {
                Log?.Invoke($"ConfigurationManager save '{ConfigFilename}'");
                mConfigFileChangedWatcher.EnableRaisingEvents = false;
                var serializer = new XmlSerializer(typeof(T));
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilename));
                using (var writer = XmlWriter.Create(ConfigFilename, new XmlWriterSettings() { Indent = true, IndentChars = "  " }))
                {
                    serializer.Serialize(writer, Current);
                }
                Log?.Invoke($"ConfigurationManager saved '{ConfigFilename}'");
            }
            catch (Exception Error)
            {
                Log?.Invoke($"ConfigurationManager save '{ConfigFilename}' error {Error}");
            }
            finally
            {
                mConfigFileChangedWatcher.EnableRaisingEvents = true;
            }
        }

    }
}
