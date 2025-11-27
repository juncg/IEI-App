using Backend;
using Serilog;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Empezando proceso...");

    // 1. Transformación: Convertir fuentes a JSON
    Log.Information("Paso 1: Iniciando transformación de datos de 'info' a 'converted_data'.");
    string infoFolder = Path.Combine(Directory.GetCurrentDirectory(), "info");
    string convertedFolder = Path.Combine(Directory.GetCurrentDirectory(), "converted_data");
    Transformations.ConvertFolderToJson(infoFolder, convertedFolder);
    Log.Information("Paso 1: Transformación de datos completada.");

    // 2. Mapeo: Unificar datos
    Log.Information("Paso 2: Iniciando mapeo de datos desde la carpeta '{ConvertedFolder}'.", convertedFolder);
    var unifiedData = await Mapper.ExecuteMapping(convertedFolder);
    Log.Information("Paso 2: Mapeo de datos completado. Total de registros mapeados: {RecordCount}", unifiedData.Count);
    
    // 3. Inserción: Poblar base de datos
    Log.Information("Paso 3: Iniciando inserción en la base de datos.");
    Inserter.Run(unifiedData);
    Log.Information("Paso 3: Inserción en la base de datos completada.");
    Log.Information("Proceso finalizado.");
}
catch (Exception ex)
{
    Log.Error(ex, "Ocurrió un error durante el proceso ETL.");
}
finally
{
    Log.CloseAndFlush();
}

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog como el logger principal
builder.Host.UseSerilog();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
