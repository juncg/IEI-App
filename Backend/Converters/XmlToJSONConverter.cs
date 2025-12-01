using Backend.Helpers;
using Newtonsoft.Json;
using Serilog;
using System.Text;
using System.Xml;

namespace Backend.Converters
{
    public class XmlToJSONConverter : IFileConverter
    {
        public bool CanConvert(string fileName)
        {
            return fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);
        }

        public void Convert(string inputFile, string outputFile)
        {
            string xmlContent = EncodingHelper.TryReadWithEncodings(inputFile);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlContent);
            string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(outputFile, json, Encoding.UTF8);
            Log.Information("Paso Converter: Archivo XML convertido a JSON: {FileName}.", Path.GetFileName(inputFile));
        }
    }
}
