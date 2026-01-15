using Backend;
using Serilog;
using System.IO;
using Backend.Api.GAL.Helpers;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using System.Globalization;
using System.Text.Json;
using System.Text;

namespace Backend.Api.GAL.Logic
{
    public static class Transformer
    {
        static Transformer()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Transforma el archivo CSV de estaciones de Galicia en un objeto JSON estructurado
        /// </summary>
        /// <returns>Objeto con los datos transformados y metadatos</returns>
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

            var galData = ConvertGalCsvToJson(galCsvPath);

            Log.Information("API GAL: Transformación completada.");

            return new
            {
                region = "Galicia",
                sourceFormat = "CSV",
                timestamp = DateTime.UtcNow,
                data = galData
            };
        }

        /// <summary>
        /// Convierte un archivo CSV de Galicia a formato JsonElement
        /// </summary>
        /// <param name="path">Ruta del archivo CSV</param>
        /// <returns>JsonElement con el contenido del archivo</returns>
        public static JsonElement ConvertGalCsvToJson(string path)
        {
            try
            {
                using var reader = new StreamReader(path, EncodingHelper.DetectEncoding(path));
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";",
                    BadDataFound = null,
                    MissingFieldFound = null
                });

                var records = csv.GetRecords<dynamic>().ToList();
                
                var wrapper = new { establishments = records };
                
                string jsonText = JsonConvert.SerializeObject(wrapper);
                
                using var document = JsonDocument.Parse(jsonText);
                return document.RootElement.Clone();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error converting GAL CSV to JSON");
                throw;
            }
        }
    }
}