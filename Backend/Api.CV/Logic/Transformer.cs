using Backend;
using Serilog;
using System.IO;

namespace Backend.Api.CV.Logic
{
    public static class Transformer
    {
        public static object Transform()
        {
            Log.Information("API CV: Iniciando transformación de datos de Comunidad Valenciana (JSON)");

            string infoFolder = Path.Combine(Directory.GetCurrentDirectory(), "info");
            string cvJsonPath = Path.Combine(infoFolder, "estaciones.json");

            if (!System.IO.File.Exists(cvJsonPath))
            {
                Log.Warning("API CV: No se encontró el archivo estaciones.json en {Path}", cvJsonPath);
                throw new FileNotFoundException("Archivo estaciones.json no encontrado");
            }

            var cvData = Transformations.ConvertCvJsonToJson(cvJsonPath);

            Log.Information("API CV: Transformación completada.");

            return new
            {
                region = "Comunidad Valenciana",
                sourceFormat = "JSON",
                timestamp = DateTime.UtcNow,
                data = cvData
            };
        }
    }
}