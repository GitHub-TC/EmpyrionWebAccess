using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Eleon.Modding;
using EmpyrionModWebHost.Configuration;
using EmpyrionModWebHost.Services;
using EmpyrionNetAPITools;
using EWAExtenderCommunication;
using FluffySpoon.AspNet.LetsEncrypt;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace EmpyrionModWebHost
{
    public interface IEWAPlugin : ModInterface
    {

    }

    public interface IClientHostCommunication
    {
        ClientMessagePipe ToEmpyrion { set; }
        void ClientHostMessage(ClientHostComData aMessage);
    }

    public interface IDatabaseConnect
    {
        void CreateAndUpdateDatabase();
    }


    public class Program
    {
        public static IWebHost Application { get; private set; }
        public static LifetimeEventsHostedService AppLifetime { get; private set; }
        public static ModHostDLL Host { get; set; }
        public static AppSettings AppSettings { get; set; }
        public static X509Certificate2 EWAStandardCertificate { get; set; }
        public static LetsEncryptACME LetsEncryptACME { get; set; }

        public static T GetManager<T>() where T : class
        {
            return Host?.Plugins?.Where(P => P is T).FirstOrDefault() as T;
        }

        public static void Main(string[] args)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(EmpyrionConfiguration.SaveGameModPath, "EWA", "DB"));

                var hostBuilder = CreateWebHostBuilder(args);
                Application = hostBuilder.Build();

                AppLifetime = Application.Services.GetService(typeof(LifetimeEventsHostedService)) as LifetimeEventsHostedService;

                SynchronizationContext.SetSynchronizationContext(Application.Services.GetService(typeof(AsyncSynchronizationContext)) as AsyncSynchronizationContext);

                Host = Application.Services.GetService(typeof(ModHostDLL)) as ModHostDLL;
                Host.InitComunicationChannels();

                Application.Run();
            }
            catch (Exception Error)
            {
                var logDir = Path.Combine(EmpyrionConfiguration.ProgramPath, "Logs", "EWA");
                Directory.CreateDirectory(logDir);
                File.AppendAllText(Path.Combine(logDir, $"{DateTime.Now:yyyyMMdd HHmm}_ewa_crash.log"), $"{DateTime.Now:yyyyMMdd HHmm}: {Error}");
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddNewtonsoftJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddNewtonsoftJsonFile(Path.Combine(EmpyrionConfiguration.SaveGameModPath, "EWA", "appsettings.json"), optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .UseKestrel(kestrelOptions => kestrelOptions.ConfigureHttpsDefaults(
            httpsOptions => httpsOptions.ServerCertificateSelector =
                (c, s) => LetsEncryptACME != null && LetsEncryptACME.UseLetsEncrypt
                    ? (LetsEncryptRenewalService.Certificate ?? EWAStandardCertificate)
                    : EWAStandardCertificate
                ))
            .UseStartup<Startup>();

        public static void CreateTempPath()
        {
            try
            {
                var temp = Environment.GetEnvironmentVariable("ASPNETCORE_TEMP") ??     // ASPNETCORE_TEMP - User set temporary location.
                           Path.GetTempPath();

                Directory.CreateDirectory(temp);
            }
            catch { }
        }

    }
}
