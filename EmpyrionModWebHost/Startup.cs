using AutoMapper;
using Community.AspNetCore.ExceptionHandling;
using Community.AspNetCore.ExceptionHandling.Mvc;
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
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
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

            services.AddLogging(lb =>
            {
                lb.AddConfiguration(Configuration.GetSection("Logging"));
                lb.AddFile(o => 
                {
                    var logDir = Path.Combine(EmpyrionConfiguration.ProgramPath, "Logs", "EWA");
                    Directory.CreateDirectory(logDir);
                    o.FallbackFileName = $"{DateTime.Now.ToString("yyyyMMdd HHmm")}_ewa.log";
                    o.RootPath       = logDir;
                });
            });

            services.AddOData();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddAutoMapper();
            services.AddSignalR();

            services.AddExceptionHandlingPolicies(options =>
            {
                //options.For<InitializationException>().Rethrow();

                //options.For<SomeTransientException>().Retry(ro => ro.MaxRetryCount = 2).NextPolicy();

                //options.For<SomeBadRequestException>()
                //.Response(e => 400)
                //    .Headers((h, e) => h["X-MyCustomHeader"] = e.Message)
                //    .WithBody((req, sw, exception) =>
                //    {
                //        byte[] array = Encoding.UTF8.GetBytes(exception.ToString());
                //        return sw.WriteAsync(array, 0, array.Length);
                //    })
                //.NextPolicy();

                // Ensure that all exception types are handled by adding handler for generic exception at the end.
                options.For<Exception>()
                .Log(lo =>
                {
                    lo.EventIdFactory = (c, e) => new EventId(123, "UnhandlerException");
                    lo.Category = (context, exception) => "EWA";
                })
                .Response(null, ResponseAlreadyStartedBehaviour.GoToNextHandler)
                    .ClearCacheHeaders()
                    .WithObjectResult((r, e) => new { msg = e.Message, path = r.Path })
                .Handled();
            });
            services.AddSingleton<LifetimeEventsHostedService>();

            services.AddSingleton<ModHostDLL>();
            services.AddSingleton(typeof(IEWAPlugin), typeof(ChatManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(PlayerManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(BackpackManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(FactionManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(SysteminfoManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(UserManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(GameplayManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(StructureManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(FactoryManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(BackupManager));
            services.AddSingleton(typeof(IEWAPlugin), typeof(TimetableManager));

            services.AddDbContext<PlayerContext>();
            services.AddDbContext<BackpackContext>();
            services.AddDbContext<FactionContext>();
            services.AddDbContext<ChatContext>();
            services.AddDbContext<UserContext>();

            // configure strongly typed settings objects
            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            // configure jwt authentication
            Program.AppSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.ASCII.GetBytes(Program.AppSettings.Secret);

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
            app.UseExceptionHandlingPolicies();

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
