using Backend;
using Serilog;
using System.IO;
using Backend.Api.CV.Helpers;
using System.Text.Json;
using System.Text;

namespace Backend.Api.CV.Logic
{
    public static class Transformer
    {
        static Transformer()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Transforma el archivo JSON de estaciones de la Comunidad Valenciana en un objeto JSON estructurado
        /// </summary>
        /// <returns>Objeto con los datos transformados y metadatos</returns>
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

            var cvData = ConvertCvJsonToJson(cvJsonPath);

            Log.Information("API CV: Transformación completada.");

            return new
            {
                region = "Comunidad Valenciana",
                sourceFormat = "JSON",
                timestamp = DateTime.UtcNow,
                data = cvData
            };
        }

        /// <summary>
        /// Convierte un archivo JSON de la Comunidad Valenciana a formato JsonElement
        /// </summary>
        /// <param name="path">Ruta del archivo JSON</param>
        /// <returns>JsonElement con el contenido del archivo</returns>
        public static JsonElement ConvertCvJsonToJson(string path)
        {
            try
            {
                string jsonContent = EncodingHelper.TryReadWithEncodings(path);
                using var document = JsonDocument.Parse(jsonContent);
                return document.RootElement.Clone();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error converting CV JSON to JSON");
                throw;
            }
        }
    }
}