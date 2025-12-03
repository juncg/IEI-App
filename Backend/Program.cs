using Backend;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    // Crear las tareas para las diferentes APIs
    var apiTasks = new List<Task>
    {
        Task.Run(() => StartCatApi()),
        Task.Run(() => StartCvApi()),
        Task.Run(() => StartGalApi())
    };

    Log.Information("Iniciando APIs de transformación...");

    // Esperar a que todas las APIs estén corriendo
    await Task.WhenAll(apiTasks);
}
catch (Exception ex)
{
    Log.Error(ex, "Error iniciando las APIs");
}
finally
{
    Log.CloseAndFlush();
}

// API de Cataluña (Puerto 5001)
static async Task StartCatApi()
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls("http://localhost:5001");

    builder.Host.UseSerilog();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "API Cataluña - Transformación XML", Version = "v1" });
    });

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API CAT v1");
    });

    app.MapGet("/api/cat/transform", () =>
    {
        try
        {
            Log.Information("API CAT: Iniciando transformación de datos de Cataluña (XML)");

            string infoFolder = Path.Combine(Directory.GetCurrentDirectory(), "info");
            string catXmlPath = Path.Combine(infoFolder, "CAT.xml");

            if (!File.Exists(catXmlPath))
            {
                Log.Warning("API CAT: No se encontró el archivo CAT.xml en {Path}", catXmlPath);
                return Results.NotFound(new { error = "Archivo CAT.xml no encontrado" });
            }

            // Leer y transformar el XML
            var catData = Transformations.ConvertCatXmlToJson(catXmlPath);

            Log.Information("API CAT: Transformación completada. {Count} registros procesados",
                catData?.GetProperty("establishments").GetArrayLength() ?? 0);

            return Results.Ok(new
            {
                region = "Cataluña",
                sourceFormat = "XML",
                timestamp = DateTime.UtcNow,
                data = catData
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "API CAT: Error durante la transformación");
            return Results.Problem(ex.Message);
        }
    })
    .WithName("TransformCataluña")
    .WithDescription("Transforma datos de establecimientos de Cataluña desde XML a JSON")
    .WithTags("Cataluña");

    Log.Information("API de Cataluña iniciada en http://localhost:5001");
    Log.Information("Swagger UI disponible en http://localhost:5001/swagger");

    await app.RunAsync();
}

// API de Comunidad Valenciana (Puerto 5002)
static async Task StartCvApi()
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls("http://localhost:5002");

    builder.Host.UseSerilog();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "API Comunidad Valenciana - Transformación JSON", Version = "v1" });
    });

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API CV v1");
    });

    app.MapGet("/api/cv/transform", () =>
    {
        try
        {
            Log.Information("API CV: Iniciando transformación de datos de Comunidad Valenciana (JSON)");

            string infoFolder = Path.Combine(Directory.GetCurrentDirectory(), "info");
            string cvJsonPath = Path.Combine(infoFolder, "CV.json");

            if (!File.Exists(cvJsonPath))
            {
                Log.Warning("API CV: No se encontró el archivo CV.json en {Path}", cvJsonPath);
                return Results.NotFound(new { error = "Archivo CV.json no encontrado" });
            }

            // Leer y transformar el JSON
            var cvData = Transformations.ConvertCvJsonToJson(cvJsonPath);

            Log.Information("API CV: Transformación completada. {Count} registros procesados",
                cvData?.GetProperty("rows").GetArrayLength() ?? 0);

            return Results.Ok(new
            {
                region = "Comunidad Valenciana",
                sourceFormat = "JSON",
                timestamp = DateTime.UtcNow,
                data = cvData
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "API CV: Error durante la transformación");
            return Results.Problem(ex.Message);
        }
    })
    .WithName("TransformComunitatValenciana")
    .WithDescription("Transforma datos de establecimientos de Comunidad Valenciana desde JSON a JSON")
    .WithTags("Comunidad Valenciana");

    Log.Information("API de Comunidad Valenciana iniciada en http://localhost:5002");
    Log.Information("Swagger UI disponible en http://localhost:5002/swagger");

    await app.RunAsync();
}

// API de Galicia (Puerto 5003)
static async Task StartGalApi()
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls("http://localhost:5003");

    builder.Host.UseSerilog();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "API Galicia - Transformación CSV", Version = "v1" });
    });

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API GAL v1");
    });

    app.MapGet("/api/gal/transform", () =>
    {
        try
        {
            Log.Information("API GAL: Iniciando transformación de datos de Galicia (CSV)");

            string infoFolder = Path.Combine(Directory.GetCurrentDirectory(), "info");
            string galCsvPath = Path.Combine(infoFolder, "GAL.csv");

            if (!File.Exists(galCsvPath))
            {
                Log.Warning("API GAL: No se encontró el archivo GAL.csv en {Path}", galCsvPath);
                return Results.NotFound(new { error = "Archivo GAL.csv no encontrado" });
            }

            // Leer y transformar el CSV
            var galData = Transformations.ConvertGalCsvToJson(galCsvPath);

            Log.Information("API GAL: Transformación completada. {Count} registros procesados",
                galData?.GetProperty("establishments").GetArrayLength() ?? 0);

            return Results.Ok(new
            {
                region = "Galicia",
                sourceFormat = "CSV",
                timestamp = DateTime.UtcNow,
                data = galData
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "API GAL: Error durante la transformación");
            return Results.Problem(ex.Message);
        }
    })
    .WithName("TransformGalicia")
    .WithDescription("Transforma datos de establecimientos de Galicia desde CSV a JSON")
    .WithTags("Galicia");

    Log.Information("API de Galicia iniciada en http://localhost:5003");
    Log.Information("Swagger UI disponible en http://localhost:5003/swagger");

    await app.RunAsync();
}
