using Backend;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Paso Main: Iniciando proceso principal...");

    // 1. Transformación: Convertir fuentes a JSON
    Log.Information("Paso Main: Iniciando transformación de datos de 'info' a 'converted_data'.");
    string infoFolder = Path.Combine(Directory.GetCurrentDirectory(), "info");
    string convertedFolder = Path.Combine(Directory.GetCurrentDirectory(), "converted_data");
    Transformations.ConvertFolderToJson(infoFolder, convertedFolder);
    Log.Information("Paso Main: Transformación de datos completada.");

    Console.WriteLine("¿Quieres comprobar las coordenadas existentes con las de selenium? (s/n)");
    string? response = Console.ReadLine();
    bool validateExistingCoordinates = response?.Trim().ToLower() == "s";

    // 2. Mapeo: Unificar datos
    Log.Information("Paso Main: Iniciando mapeo de datos desde la carpeta '{ConvertedFolder}'.", convertedFolder);
    var mapperService = new Backend.Services.MapperService();
    var unifiedData = await mapperService.ExecuteMapping(convertedFolder, validateExistingCoordinates);
    Log.Information("Paso Main: Mapeo de datos completado. Total de registros mapeados: {RecordCount}", unifiedData.Count);

    // 3. Inserción: Poblar base de datos
    Log.Information("Paso Main: Iniciando inserción en la base de datos.");
    var inserter = new Backend.Services.DataInserter();
    inserter.Run(unifiedData);
    Log.Information("Paso Main: Inserción en la base de datos completada.");
    Log.Information("Paso Main: Proceso principal finalizado.");
}
catch (Exception ex)
{
    Log.Error(ex, "Paso Main: Ocurrió un error durante el proceso principal ETL.");
}
finally
{
    Log.CloseAndFlush();
}

var builder = WebApplication.CreateBuilder(args);

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
