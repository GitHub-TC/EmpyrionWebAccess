using System;
using System.Threading;
using System.Threading.Tasks;

namespace EmpyrionModWebHost.Extensions
{
    public static class TaskTools
    {
        public static void Retry(Action action)
        {
            Retry(10, 1000, action);
        }

        public static void Retry(int count, Action action)
        {
            Retry(count, 1000, action);
        }

        public static void Retry(int count, int millisecondsTimeout, Action action)
        {
            for (int i = 1; i <= count; i++)
            {
                try
                {
                    action();
                }
                catch
                {
                    if (i == count) throw;
                    Thread.Sleep(millisecondsTimeout);
                }
            }
        }

        public static T Retry<T>(Func<T> func)
        {
            return Retry(10, 1000, func);
        }

        public static T Retry<T>(int count, Func<T> func)
        {
            return Retry(count, 1000, func);
        }

        public static T Retry<T>(int count, int millisecondsTimeout, Func<T> func)
        {
            for (int i = 1; i <= count; i++)
            {
                try
                {
                    return func();
                }
                catch
                {
                    if (i == count) throw;
                    Thread.Sleep(millisecondsTimeout);
                }
            }

            return default(T);
        }

        public static ManualResetEvent IntervallAsync(int aMillisecondsIntervall, Func<Task> aAction)
        {
            var localExit = new ManualResetEvent(false);
            Program.AppLifetime.StopApplicationEvent += (S, A) => localExit.Set();
            new Thread(() => {
                bool isLocalExit = false;
                while (!Program.AppLifetime.Exit && !isLocalExit)
                {
                    try
                    {
                        aAction().Wait(aMillisecondsIntervall);
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
