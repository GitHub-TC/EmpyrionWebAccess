using System;
using System.Threading;

namespace EmpyrionModWebHost.Extensions
{
    public static class TaskTools
    {

        public static void Intervall(int aMillisecondsIntervall, Action aAction)
        {
            new Thread(() => {
                while (!Program.AppLifetime.Exit)
                {
                    try
                    {
                        aAction();
                    }
                    catch (Exception Error)
                    {
                        Console.WriteLine(Error);
                    }
                    Thread.Sleep(aMillisecondsIntervall);
                }
            }).Start();
        }

        public static void Delay(int aSeconds, Action aAction)
        {
            Delay(new TimeSpan(0,0, aSeconds), aAction);
        }

        public static void Delay(TimeSpan aExecAfterTimeout, Action aAction)
        {
            new Thread(() => {
                try
                {
                Thread.Sleep((int)aExecAfterTimeout.TotalMilliseconds);
                aAction();
                }
                catch (Exception Error)
                {
                    Console.WriteLine(Error);
                }
            }).Start();
        }

    }
}
