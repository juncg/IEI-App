using Backend;
using Serilog;
using System.IO;

namespace Backend.Api.CAT.Logic
{
    public static class Transformer
    {
        public static object Transform()
        {
            Log.Information("API CAT: Iniciando transformación de datos de Cataluña (XML)");

            string infoFolder = Path.Combine(Directory.GetCurrentDirectory(), "info");
            string catXmlPath = Path.Combine(infoFolder, "ITV-CAT.xml");

            if (!System.IO.File.Exists(catXmlPath))
            {
                Log.Warning("API CAT: No se encontró el archivo ITV-CAT.xml en {Path}", catXmlPath);
                throw new FileNotFoundException("Archivo ITV-CAT.xml no encontrado");
            }

            var catData = Transformations.ConvertCatXmlToJson(catXmlPath);

            Log.Information("API CAT: Transformación completada.");

            return new
            {
                region = "Cataluña",
                sourceFormat = "XML",
                timestamp = DateTime.UtcNow,
                data = catData
            };
        }
    }
}