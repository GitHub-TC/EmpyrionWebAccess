using System.Linq;
using Eleon.Modding;
using EWAExtenderCommunication;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

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


    public class Program
    {
        public static ModHostDLL Host { get; set; }

        public static T GetManager<T>() where T : class
        {
            return Host.Plugins?.Where(P => P is T).FirstOrDefault() as T;
        } 

        public static void Main(string[] args)
        {
            var App = CreateWebHostBuilder(args).Build();

            Host = App.Services.GetService(typeof(ModHostDLL)) as ModHostDLL;
            Host.InitComunicationChannels();

            App.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
