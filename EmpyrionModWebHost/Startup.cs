using EmpyrionModWebHost.Controllers;
using EmpyrionModWebHost.Models;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace EmpyrionModWebHost
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOData();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddSignalR();

            services.AddSingleton(typeof(ModHostDLL));
            services.AddSingleton(typeof(IEWAPlugin), typeof(ChatManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(PlayerManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(BackpackManager));

            services.AddDbContext<PlayerContext>();
            services.AddDbContext<BackpackContext>();

            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseSignalR(routes => {
                routes.MapHub<ChatHub>("/hubs/chat");
                routes.MapHub<PlayerHub>("/hubs/player");
                routes.MapHub<BackpackHub>("/hubs/backpack");
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "default", template: "{controller}/{action=Index}/{id?}");
                routes.Select().Expand().Filter().OrderBy().MaxTop(1000).Count();
                routes.MapODataServiceRoute("player", "odata", GetEdmPlayerModel());
                routes.MapODataServiceRoute("backpack", "odata", GetEdmBackpackModel());
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });
        }

        private static IEdmModel GetEdmPlayerModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Player>("Players");
            return builder.GetEdmModel();

        }

        private static IEdmModel GetEdmBackpackModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Backpack>("Backpacks");
            return builder.GetEdmModel();

        }

    }
}
