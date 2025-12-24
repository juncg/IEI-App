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
        Task.Run(() => StartGalApi()),
        Task.Run(() => StartLoadApi()),
        Task.Run(() => StartSearchApi())
    };

    Log.Information("Iniciando APIs de transformación, carga y búsqueda...");

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

static async Task StartCatApi()
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls("http://localhost:5001");

    builder.Host.UseSerilog();
    builder.Services.AddControllers();
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

    app.MapControllers();

    Log.Information("API de Cataluña iniciada en http://localhost:5001");
    Log.Information("Swagger UI disponible en http://localhost:5001/swagger");

    await app.RunAsync();
}

static async Task StartCvApi()
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls("http://localhost:5002");

    builder.Host.UseSerilog();
    builder.Services.AddControllers();
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

    app.MapControllers();

    Log.Information("API de Comunidad Valenciana iniciada en http://localhost:5002");
    Log.Information("Swagger UI disponible en http://localhost:5002/swagger");

    await app.RunAsync();
}

static async Task StartGalApi()
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls("http://localhost:5003");

    builder.Host.UseSerilog();
    builder.Services.AddControllers();
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

    app.MapControllers();

    Log.Information("API de Galicia iniciada en http://localhost:5003");
    Log.Information("Swagger UI disponible en http://localhost:5003/swagger");

    await app.RunAsync();
}

static async Task StartLoadApi()
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls("http://localhost:5004");

    builder.Host.UseSerilog();
    builder.Services.AddControllers();
    builder.Services.AddHttpClient();
    builder.Services.AddCors();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "API Carga - ETL", Version = "v1" });
    });

    var app = builder.Build();

    app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Carga v1");
    });

    app.MapControllers();

    Log.Information("API de Carga iniciada en http://localhost:5004");
    Log.Information("Swagger UI disponible en http://localhost:5004/swagger");

    await app.RunAsync();
}

static async Task StartSearchApi()
{
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseUrls("http://localhost:5005");

    builder.Host.UseSerilog();
    builder.Services.AddControllers();
    builder.Services.AddScoped<Backend.Api.Busqueda.Logic.SearchService>();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "API Búsqueda", Version = "v1" });
    });

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Búsqueda v1");
    });

    app.MapControllers();

    Log.Information("API de Búsqueda iniciada en http://localhost:5005");
    Log.Information("Swagger UI disponible en http://localhost:5005/swagger");

    await app.RunAsync();
}
