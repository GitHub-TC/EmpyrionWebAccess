using AutoMapper;
using EmpyrionModWebHost.Configuration;
using EmpyrionModWebHost.Controllers;
using EmpyrionModWebHost.Migrations;
using EmpyrionModWebHost.Models;
using EmpyrionModWebHost.Services;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Routing.Conventions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;

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
            services.AddCors();

            services.AddOData();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddAutoMapper();
            services.AddSignalR();

            services.AddSingleton<LifetimeEventsHostedService>();

            services.AddSingleton<ModHostDLL>();
            services.AddSingleton(typeof(IEWAPlugin), typeof(ChatManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(PlayerManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(BackpackManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(FactionManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(SysteminfoManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(UserManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(GameplayManager));

            services.AddDbContext<PlayerContext>();
            services.AddDbContext<BackpackContext>();
            services.AddDbContext<FactionContext>();
            services.AddDbContext<ChatContext>();
            services.AddDbContext<UserContext>();

            // configure strongly typed settings objects
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            // configure jwt authentication
            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(appSettings.Secret);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                        var userId = int.Parse(context.Principal.Identity.Name);
                        var user = userService.GetById(userId);
                        if (user == null)
                        {
                            // return unauthorized if user no longer exists
                            context.Fail("Unauthorized");
                        }
                        return Task.CompletedTask;
                    }
                };
                x.RequireHttpsMetadata = true;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            // configure DI for application services
            services.AddScoped<IUserService, UserService>();

            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist/ClientApp";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            // global cors policy
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            app.UseAuthentication();

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
                routes.MapHub<FactionHub>("/hubs/faction");
                routes.MapHub<SysteminfoHub>("/hubs/systeminfo");
            });

            app.UseMvc(routes =>
            {
                routes.Select().Expand().Filter().OrderBy().MaxTop(1000).Count();
                routes.MapODataServiceRoute("player",   "odata", PlayersController  .GetEdmModel());
                routes.MapODataServiceRoute("faction",  "odata", FactionsController .GetEdmModel());
                routes.MapODataServiceRoute("chat",     "odata", ChatsController    .GetEdmModel());
                routes.EnableDependencyInjection();

                routes.MapRoute(name: "default", template: "{controller}/{action=Index}/{id?}");

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

    }
}
