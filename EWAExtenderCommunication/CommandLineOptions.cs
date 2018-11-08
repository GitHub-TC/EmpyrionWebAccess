using System;
using System.Linq;

namespace EWAExtenderCommunication
{
    public static class CommandLineOptions

    {
        public static string GetOption(string aName, string aDefaultValue)
        {
            return Environment.GetCommandLineArgs().Any(A => string.Compare(A, aName, StringComparison.InvariantCultureIgnoreCase) != 0)
                ? Environment.GetCommandLineArgs().SkipWhile(A => string.Compare(A, aName, StringComparison.InvariantCultureIgnoreCase) != 0).Skip(1).FirstOrDefault()
                : aDefaultValue;
        }

    }
}
