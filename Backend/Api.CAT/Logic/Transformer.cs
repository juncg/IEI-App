using Backend;
using Serilog;
using System.IO;
using Backend.Api.CAT.Helpers;
using Newtonsoft.Json;
using System.Text.Json;
using System.Xml;
using System.Text;

namespace Backend.Api.CAT.Logic
{
    public static class Transformer
    {
        static Transformer()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

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

            var catData = ConvertCatXmlToJson(catXmlPath);

            Log.Information("API CAT: Transformación completada.");

            return new
            {
                region = "Cataluña",
                sourceFormat = "XML",
                timestamp = DateTime.UtcNow,
                data = catData
            };
        }

        public static JsonElement ConvertCatXmlToJson(string path)
        {
            try
            {
                string xmlContent = EncodingHelper.TryReadWithEncodings(path);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);
                string jsonText = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.None);
                
                using var document = JsonDocument.Parse(jsonText);
                return document.RootElement.Clone();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error converting CAT XML to JSON");
                throw;
            }
        }
    }
}