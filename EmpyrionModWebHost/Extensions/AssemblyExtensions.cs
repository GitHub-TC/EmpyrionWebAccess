using System.Linq;
using System.Reflection;

namespace EmpyrionModWebHost.Extensions
{
    public static class AssemblyExtensions
    {
        public static T GetAttribute<T>(this Assembly aAssembly)
        {
            return aAssembly.GetCustomAttributes(typeof(T), false).OfType<T>().FirstOrDefault();
        }
    }
}
