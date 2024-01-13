using Microsoft.AspNetCore.Http;

namespace EmpyrionModWebHost.Services
{
    public class IdIconMiddleware
    {
        public RequestDelegate Next { get; }
        public IConfiguration Configuration { get; }
        public ILogger<IdIconMiddleware> Logger { get; }
        public IDictionary<int, string> IdIconMapping { get; set; }

        public IdIconMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<IdIconMiddleware> logger)
        {
            Next          = next;
            Configuration = configuration;
            Logger        = logger;
        }

        IDictionary<int, string> ReadIdIconMapping()
        {
            var idIconMappingFile = Configuration?.GetValue<string>("IdIconMappingFile");
            idIconMappingFile = File.Exists(idIconMappingFile) ? idIconMappingFile : Path.Combine(EmpyrionConfiguration.ModPath, @"EWALoader\EWA\IdIconMapping.json");

            try
            {
                Logger.LogInformation("IdIconMapping:{idIconMappingFile}", idIconMappingFile);

                using var file = File.OpenRead(idIconMappingFile);

                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<int, string>>(file);
            }
            catch (Exception error)
            {
                Logger.LogError(error, "IdIconMapping:{idIconMappingFile}", idIconMappingFile);

                return new Dictionary<int, string>();
            }
        }

        public async Task InvokeAsync(
            HttpContext context)
        {
            if(IdIconMapping == null) IdIconMapping = ReadIdIconMapping();

            if (IdIconMapping == null)
            {
                await Next(context);
                return;
            }

            var path = context.Request.Path.ToString();

            if (path.Contains("/Items/"))
            {
                if(!int.TryParse(Path.GetFileNameWithoutExtension(path), out var id))
                {
                    await Next(context);
                    return;
                }

                var iconFile = IdIconMapping.TryGetValue(id, out var icon) ? icon : id.ToString();

                var iconFilePath = Path.Combine(EmpyrionConfiguration.ProgramPath, "Content", "Scenarios", EmpyrionConfiguration.DedicatedYaml.CustomScenarioName ?? string.Empty, @"SharedData\Content\Bundles\ItemIcons", $"{iconFile}.png");
                if(!File.Exists(iconFilePath)) iconFilePath = Path.Combine(EmpyrionConfiguration.ModPath, @"EWALoader\EWA\ClientApp\dist\ClientApp\assets\Items", $"{iconFile}.png");

                // Switch to default Tokenicon
                if (id >= 100000 && !File.Exists(iconFilePath)) iconFilePath = Path.Combine(EmpyrionConfiguration.ModPath, @"EWALoader\EWA\ClientApp\dist\ClientApp\assets\Items", "KeyCardBlack.png");

                if (!File.Exists(iconFilePath))
                {
                    await Next(context);
                    return;
                }

                context.Response.ContentType = "image/png";
                await context.Response.BodyWriter.WriteAsync(File.ReadAllBytes(iconFilePath));

                return;
            }

            await Next(context);
        }
    }
}
