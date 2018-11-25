using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmpyrionModWebHost.Services
{
    public class LifetimeEventsHostedService : IHostedService
    {
        private readonly ILogger _logger;
        public IApplicationLifetime AppLifetime { get; private set; }

        public bool Exit { get; private set; }

        public LifetimeEventsHostedService(
            ILogger<LifetimeEventsHostedService> logger, IApplicationLifetime appLifetime)
        {
            _logger = logger;
            AppLifetime = appLifetime;

            AppLifetime.ApplicationStarted.Register(OnStarted);
            AppLifetime.ApplicationStopping.Register(OnStopping);
            AppLifetime.ApplicationStopped.Register(OnStopped);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void StopApplication()
        {
            AppLifetime.StopApplication();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");

            // Perform post-startup activities here
        }

        private void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
            Exit = true;
            // Perform on-stopping activities here
        }

        private void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");

            // Perform post-stopped activities here
        }
    }
}
