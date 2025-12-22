using Backend;
using Serilog;
using System.IO;

namespace Backend.Api.GAL.Logic
{
    public static class Transformer
    {
        public static object Transform()
        {
            Log.Information("API GAL: Iniciando transformación de datos de Galicia (CSV)");

            string infoFolder = Path.Combine(Directory.GetCurrentDirectory(), "info");
            string galCsvPath = Path.Combine(infoFolder, "Estacions_ITV.csv");

            if (!System.IO.File.Exists(galCsvPath))
            {
                Log.Warning("API GAL: No se encontró el archivo Estacions_ITV.csv en {Path}", galCsvPath);
                throw new FileNotFoundException("Archivo Estacions_ITV.csv no encontrado");
            }

            var galData = Transformations.ConvertGalCsvToJson(galCsvPath);

            Log.Information("API GAL: Transformación completada.");

            return new
            {
                region = "Galicia",
                sourceFormat = "CSV",
                timestamp = DateTime.UtcNow,
                data = galData
            };
        }
    }
}