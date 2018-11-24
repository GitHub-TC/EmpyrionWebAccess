using System;
using System.Threading;
using System.Threading.Tasks;

namespace EmpyrionModWebHost.Extensions
{
    public static class TaskExtensions
    {
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, int timeout)
        {
            return await task.TimeoutAfter(new TimeSpan(0, 0, 0, 0, timeout));
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {

            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {

                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask == task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;  // Very important in order to propagate exceptions
                }
                else
                {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }

        public static void Intervall(int aIntervall, Action aAction)
        {
            new Thread(() => {
                while (true)
                {
                    try
                    {
                        aAction();
                    }
                    catch (Exception Error)
                    {
                        Console.WriteLine(Error);
                    }
                    Thread.Sleep(aIntervall);
                }
            }).Start();
        }

    }
}
