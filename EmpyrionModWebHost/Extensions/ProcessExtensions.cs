using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EmpyrionModWebHost.Extensions
{
    public static class ProcessExtensions
    {
        public static IEnumerable<Process> GetChildProcesses(this Process process)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetChildProcessesWindows(process);
            return GetChildProcessesLinux(process);
        }

        private static IEnumerable<Process> GetChildProcessesWindows(Process process)
        {
            try
            {
                // Use reflection to avoid hard dependency on System.Management on Linux
                var type = Type.GetType("System.Management.ManagementObjectSearcher, System.Management");
                if (type == null) return Enumerable.Empty<Process>();
                var searcher = Activator.CreateInstance(type, $"Select * From Win32_Process Where ParentProcessID={process.Id}");
                var getMethod = type.GetMethod("Get", Type.EmptyTypes);
                var collection = getMethod?.Invoke(searcher, null);
                if (collection == null) return Enumerable.Empty<Process>();
                var children = new List<Process>();
                foreach (var mo in (IEnumerable<dynamic>)collection)
                    children.Add(Process.GetProcessById((int)(uint)mo["ProcessID"]));
                return children;
            }
            catch { return Enumerable.Empty<Process>(); }
        }

        private static IEnumerable<Process> GetChildProcessesLinux(Process process)
        {
            var children = new List<Process>();
            try
            {
                foreach (var dir in Directory.GetDirectories("/proc"))
                {
                    var statusFile = Path.Combine(dir, "status");
                    if (!File.Exists(statusFile)) continue;
                    var lines = File.ReadAllLines(statusFile);
                    var ppidLine = lines.FirstOrDefault(l => l.StartsWith("PPid:"));
                    if (ppidLine == null) continue;
                    if (int.TryParse(ppidLine.Split(':')[1].Trim(), out int ppid) && ppid == process.Id)
                    {
                        var name = Path.GetFileName(dir);
                        if (int.TryParse(name, out int pid))
                        {
                            try { children.Add(Process.GetProcessById(pid)); } catch { }
                        }
                    }
                }
            }
            catch { }
            return children;
        }
    }
}
