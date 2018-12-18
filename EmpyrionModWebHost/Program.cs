using System.IO;
using System.Linq;
using Eleon.Modding;
using EmpyrionModWebHost.Configuration;
using EmpyrionModWebHost.Migrations;
using EmpyrionModWebHost.Services;
using EWAExtenderCommunication;
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
        public static AppSettings AppSettings { get; internal set; }

        public static T GetManager<T>() where T : class
        {
            return Host.Plugins?.Where(P => P is T).FirstOrDefault() as T;
        }

        public static void Main(string[] args)
        {
            Directory.CreateDirectory(Path.Combine(EmpyrionConfiguration.SaveGameModPath, "DB"));

            Application = CreateWebHostBuilder(args).Build();

            AppLifetime =  Application.Services.GetService(typeof(LifetimeEventsHostedService)) as LifetimeEventsHostedService;

            Host = Application.Services.GetService(typeof(ModHostDLL)) as ModHostDLL;
            Host.InitComunicationChannels();

            Application.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true)
                      .AddJsonFile(Path.Combine(EmpyrionConfiguration.SaveGameModPath, "appsettings.json"), optional: true);
                config.AddEnvironmentVariables();
            })
            .UseStartup<Startup>();
    }
}
