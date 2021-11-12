namespace EmpyrionModWebHost;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }
    public string LogFileName { 
        get {
            if (_LogFileName != null) return _LogFileName;

            _LogFileName = Path.Combine(Path.Combine(EmpyrionConfiguration.ProgramPath, "Logs", "EWA"), $"{DateTime.Now.ToString("yyyyMMdd HHmm")}_ewa.log");
            Directory.CreateDirectory(Path.GetDirectoryName(_LogFileName));
            return _LogFileName;
        }
    }
    string _LogFileName;

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCors();

        services.AddMvc(options =>
                {
                    options.EnableEndpointRouting = false;
                })
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddAutoMapper(typeof(Startup));
        services.AddSignalR();

        services.AddExceptionHandlingPolicies(options =>
        {
                options.For<Exception>()
            .Log(lo =>
            {
                lo.Category = (context, exception) => "EWA";
            })
            .Response(null, ResponseAlreadyStartedBehaviour.GoToNextHandler)
                .ClearCacheHeaders()
                .WithObjectResult((r, e) => new { msg = e.Message, path = r.Path })
            .Handled();
        });
        services.AddSingleton<LifetimeEventsHostedService>();
        services.AddSingleton<AsyncSynchronizationContext>();

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddControllers()
            .AddOData(opt =>
            {
                var builder = new ODataConventionModelBuilder();
                PlayersController   .GetEdmModel(builder);
                ChatsController     .GetEdmModel(builder);
                FactionsController  .GetEdmModel(builder);

                opt.Select().Expand().Filter().OrderBy().SetMaxTop(1000).Count();
                opt.AddRouteComponents("odata", builder.GetEdmModel());
            })
            .AddJsonOptions(opt =>
            {
                opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

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
        services.AddSingleton(typeof(IEWAPlugin), typeof(PlayfieldManager));
        services.AddSingleton(typeof(IEWAPlugin), typeof(ModManager));
        services.AddSingleton(typeof(IEWAPlugin), typeof(HistoryBookManager));
        services.AddSingleton(typeof(IEWAPlugin), typeof(SectorsManager));

        services.AddTransient<IProvider<IUserService>, ServiceInstanceProvider<IUserService>>();
        services.AddTransient<IRoleHubContext<ChatHub>,   RoleHub<ChatHub>>();
        services.AddTransient<IRoleHubContext<PlayerHub>, RoleHub<PlayerHub>>();

        services.AddDbContext<PlayerContext>();
        services.AddDbContext<BackpackContext>();
        services.AddDbContext<FactionContext>();
        services.AddDbContext<ChatContext>();
        services.AddDbContext<UserContext>();
        services.AddDbContext<HistoryBookContext>();
        services.AddDbContext<FactoryItemsContext>();

        // LetsEncryptACME config
        var LetsEncryptACMESection = Configuration.GetSection("LetsEncryptACME");
        services.Configure<AppSettings>(LetsEncryptACMESection);
        Program.LetsEncryptACME = LetsEncryptACMESection.Get<LetsEncryptACME>();

        // configure strongly typed settings objects
        var appSettingsSection = Configuration.GetSection("AppSettings");
        services.Configure<AppSettings>(appSettingsSection);

        // configure jwt authentication
        Program.AppSettings = appSettingsSection.Get<AppSettings>();
        var key = Encoding.ASCII.GetBytes(Program.AppSettings.Secret);

        var CertificatePath     = Configuration.GetValue<string>("Kestrel:Certificates:Default:Path",     "EmpyrionWebAccess.pfx");
        var CertificatePassword = Configuration.GetValue<string>("Kestrel:Certificates:Default:Password", "ae28f963219c38b682b75bd2b281e0c64796e341ae74b8a5bfcdc169e817eefc");

        try
        {
            //throw new Exception("test");
            Program.EWAStandardCertificate = new X509Certificate2(
                Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).Location), CertificatePath),
                CertificatePassword,
                X509KeyStorageFlags.UserKeySet      |
                X509KeyStorageFlags.PersistKeySet   |
                X509KeyStorageFlags.Exportable
                );
        }
        catch (Exception error)
        {
            if (!Program.LetsEncryptACME.UseLetsEncrypt)
            {
                File.AppendAllText(LogFileName, $"ERROR: (switch to unsecure HTTP) Program.EWAStandardCertificate = new X509Certificate2 -> {error}");
                Program.AppSettings.UseHttpsRedirection = false;
            }
        }

        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                    var userId = int.Parse(context.Principal.Identity.Name);
                    userService.CurrentUser = userService.GetById(userId);
                        
                    if (userService.CurrentUser == null || userService.CurrentUser.Role == Role.None)
                    {
                        context.Fail("Unauthorized");
                    }
                    else
                    {
                        for (int r = (int)userService.CurrentUser.Role; r < (int)Role.None; r++)
                        {
                            ((ClaimsIdentity)context.Principal.Identity).AddClaims(new[] { new Claim(ClaimTypes.Role, ((Role)r).ToString()) });
                        }
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

        if (Program.LetsEncryptACME.UseLetsEncrypt)
        {
            //the following line adds the automatic renewal service.
            services.AddFluffySpoonLetsEncryptRenewalService(new LetsEncryptOptions()
            {
                Email = Program.LetsEncryptACME.EmailAddress, // "nitrado@cl-mail.eu", //LetsEncrypt will send you an e-mail here when the certificate is about to expire
                UseStaging = false, //switch to true for testing
                Domains = new[] { Program.LetsEncryptACME.DomainToUse },
                CertificateFriendlyName = Program.LetsEncryptACME.CertificateFriendlyName,
                TimeUntilExpiryBeforeRenewal = TimeSpan.FromDays(30), //renew automatically 30 days before expiry
                //TimeAfterIssueDateBeforeRenewal = TimeSpan.FromDays(7), //renew automatically 7 days after the last certificate was issued
                CertificateSigningRequest = new CsrInfo() //these are your certificate details
                {
                    CountryName         = Program.LetsEncryptACME.CountryName,
                    Locality            = Program.LetsEncryptACME.Locality,
                    Organization        = Program.LetsEncryptACME.Organization,
                    OrganizationUnit    = Program.LetsEncryptACME.OrganizationUnit,
                    State               = Program.LetsEncryptACME.State
                }
            });

            var StorePath = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "EWA", "LetsEncrypt");
            try { Directory.CreateDirectory(StorePath); } catch { }

            //the following line tells the library to persist the certificate to a file, so that if the server restarts, the certificate can be re-used without generating a new one.
            services.AddFluffySpoonLetsEncryptFileCertificatePersistence(Path.Combine(StorePath, "Certificate"));

            //the following line tells the library to persist challenges in-memory. challenges are the "/.well-known" URL codes that LetsEncrypt will call.
            services.AddFluffySpoonLetsEncryptMemoryChallengePersistence();
        }
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        loggerFactory.AddFile(LogFileName);

        // global cors policy
        app.UseCors(x => x
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            );
            
        app.UseExceptionHandlingPolicies();
        if (Program.LetsEncryptACME.UseLetsEncrypt) app.UseFluffySpoonLetsEncryptChallengeApprovalMiddleware();

        app.UseAuthentication();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            if (Program.AppSettings.UseHttpsRedirection) app.UseHsts();
        }

        if(Program.AppSettings.UseHttpsRedirection) app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseSpaStaticFiles();
        app.UseErrorHandlingMiddleware();

        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints => {
            endpoints.MapHub<ChatHub>      ("/hubs/chat");
            endpoints.MapHub<PlayerHub>    ("/hubs/player");
            endpoints.MapHub<BackpackHub>  ("/hubs/backpack");
            endpoints.MapHub<FactionHub>   ("/hubs/faction");
            endpoints.MapHub<SysteminfoHub>("/hubs/systeminfo");
            endpoints.MapHub<PlayfieldHub> ("/hubs/playfield");
            endpoints.MapHub<ModinfoHub>   ("/hubs/modinfo");

            endpoints.MapControllers();
        });

        app.UseMvc(routes =>
        {
            //routes.Select().Expand().Filter().OrderBy().MaxTop(1000).Count();
            //routes.MapODataServiceRoute("player",   "odata", PlayersController  .GetEdmModel());
            //routes.MapODataServiceRoute("faction",  "odata", FactionsController .GetEdmModel());
            //routes.MapODataServiceRoute("chat",     "odata", ChatsController    .GetEdmModel());
            //routes.EnableDependencyInjection();

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
