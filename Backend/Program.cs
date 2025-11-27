using Backend;
using Serilog;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting ETL Process...");

    // 1. Transformación: Convertir fuentes a JSON
    Log.Information("Step 1: Starting data transformation from 'info' to 'converted_data'.");
    string infoFolder = Path.Combine(Directory.GetCurrentDirectory(), "info");
    string convertedFolder = Path.Combine(Directory.GetCurrentDirectory(), "converted_data");
    Transformations.ConvertFolderToJson(infoFolder, convertedFolder);
    Log.Information("Step 1: Data transformation completed.");

    // 2. Mapeo: Unificar datos
    Log.Information("Step 2: Starting data mapping from folder '{ConvertedFolder}'.", convertedFolder);
    var unifiedData = await Mapper.ExecuteMapping(convertedFolder);
    Log.Information("Step 2: Data mapping completed. Total records mapped: {RecordCount}", unifiedData.Count);

    // 3. Inserción: Poblar base de datos
    Log.Information("Step 3: Starting database insertion.");
    Inserter.Run(unifiedData);
    Log.Information("Step 3: Database insertion completed.");

    Log.Information("ETL Process Finished.");
}
catch (Exception ex)
{
    Log.Error(ex, "An error occurred during the ETL process.");
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
