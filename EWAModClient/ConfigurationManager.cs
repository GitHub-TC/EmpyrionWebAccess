using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace EWAModClient
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

        FileSystemWatcher ConfigFileChangedWatcher { get; set; }

        public T Current { get; set; }

        public static Action<string> Log { get; set; }

        private void ActivateFileChangeWatcher()
        {
            if (ConfigFileChangedWatcher != null) ConfigFileChangedWatcher.EnableRaisingEvents = false;
            ConfigFileChangedWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(ConfigFilename),
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = Path.GetFileName(ConfigFilename)
            };
            ConfigFileChangedWatcher.Changed += (s, e) => Load();
            ConfigFileChangedWatcher.EnableRaisingEvents = true;
        }

        public void Load()
        {
            try
            {
                Log?.Invoke($"ConfigurationManager load '{ConfigFilename}'");
                var serializer = new XmlSerializer(typeof(T));
                using (var reader = XmlReader.Create(ConfigFilename)) Current = (T)serializer.Deserialize(reader);
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
                ConfigFileChangedWatcher.EnableRaisingEvents = false;
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
                ConfigFileChangedWatcher.EnableRaisingEvents = true;
            }
        }

    }
}
