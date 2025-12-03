using Backend.Converters;
using Backend.Helpers;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Serilog;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace Backend
{
    public class Transformations
    {
        static Transformations()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
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