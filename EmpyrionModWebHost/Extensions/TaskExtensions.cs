using System;
using System.Threading;

namespace EmpyrionModWebHost.Extensions
{
    public static class TaskTools
    {

        public static ManualResetEvent Intervall(int aMillisecondsIntervall, Action aAction)
        {
            var localExit = new ManualResetEvent(false);
            Program.AppLifetime.StopApplicationEvent += (S, A) => localExit.Set();
            new Thread(() => {
                bool isLocalExit = false;
                while (!Program.AppLifetime.Exit && !isLocalExit)
                {
                    try
                    {
                        aAction();
                    }
                    catch (Exception Error)
                    {
                        Console.WriteLine(Error);
                    }
                    isLocalExit = localExit.WaitOne(aMillisecondsIntervall);
                }
            }).Start();
            return localExit;
        }

        public static void Delay(int aSeconds, Action aAction)
        {
            Delay(new TimeSpan(0,0, aSeconds), aAction);
        }

        public static ManualResetEvent Delay(TimeSpan aExecAfterTimeout, Action aAction)
        {
            var localExit = new ManualResetEvent(false);
            Program.AppLifetime.StopApplicationEvent += (S, A) => localExit.Set();
            new Thread(() => {
                try
                {
                    localExit.WaitOne(aExecAfterTimeout);
                    aAction();
                }
                catch (Exception Error)
                {
                    Console.WriteLine(Error);
                }
            }).Start();
            return localExit;
        }

    }
}
