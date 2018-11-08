using System;
using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Eleon.Modding;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EmpyrionModWebHost
{
    public interface IEWAPlugin : ModInterface
    {

    }


    public class Program
    {
        public static ModHostDLL Host { get; set; }
        public static CompositionHost CompositionContainer { get; private set; }

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

        private static void Compose()
        {
            var rules = new ConventionBuilder();
            rules.ForTypesDerivedFrom<ModHostDLL>()
                .Export<ModHostDLL>()
                .Shared();

            var configuration = new ContainerConfiguration()
                .WithAssemblies(new[] { Assembly.GetExecutingAssembly() }, rules);
            CompositionContainer = configuration.CreateContainer();

            //Host = CompositionContainer.GetExport<ModHostDLL>();
        }
    }
}
