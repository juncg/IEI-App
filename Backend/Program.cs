using Backend;
using Serilog;
using System.IO;

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
        c.SwaggerDoc("CAT", new() { Title = "API Cataluña - Transformación XML", Version = "v1" });
        c.DocInclusionPredicate((name, api) => api.GroupName == name);
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
    });

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/CAT/swagger.json", "API CAT v1");
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
        c.SwaggerDoc("CV", new() { Title = "API Comunidad Valenciana - Transformación JSON", Version = "v1" });
        c.DocInclusionPredicate((name, api) => api.GroupName == name);
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
    });

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/CV/swagger.json", "API CV v1");
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
        c.SwaggerDoc("GAL", new() { Title = "API Galicia - Transformación CSV", Version = "v1" });
        c.DocInclusionPredicate((name, api) => api.GroupName == name);
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
    });

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/GAL/swagger.json", "API GAL v1");
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
    builder.Services.AddScoped<Backend.Api.Carga.Logic.LoadService>();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("Load", new() { Title = "API Carga - ETL", Version = "v1" });
        c.DocInclusionPredicate((name, api) => api.GroupName == name);
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
    });

    var app = builder.Build();

    app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/Load/swagger.json", "API Carga v1");
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
        c.SwaggerDoc("Search", new() { Title = "API Búsqueda", Version = "v1" });
        c.DocInclusionPredicate((name, api) => api.GroupName == name);
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);
    });

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/Search/swagger.json", "API Búsqueda v1");
    });

    app.MapControllers();

    Log.Information("API de Búsqueda iniciada en http://localhost:5005");
    Log.Information("Swagger UI disponible en http://localhost:5005/swagger");

    await app.RunAsync();
}
