using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace EmpyrionModWebHost.Services
{
    public class AsyncSynchronizationContext : SynchronizationContext
    {
        public ILogger<AsyncSynchronizationContext> Logger { get; set; }

        public AsyncSynchronizationContext(ILogger<AsyncSynchronizationContext> logger)
        {
            Logger = logger;
        }

        public override void Send(SendOrPostCallback action, object state)
        {
            try
            {
                action(state);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "AsyncSynchronizationContext:Send:Exception {0}", state);
            }
        }

        public override void Post(SendOrPostCallback action, object state)
        {
            try
            {
                action(state);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "AsyncSynchronizationContext:Post:Exception {0}", state);
            }
        }
    }
}
