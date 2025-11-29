using Backend.Helpers;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Serilog;
using System.Globalization;
using System.Text;

namespace Backend.Converters
{
    public class CsvToJSONConverter : IFileConverter
    {
        public bool CanConvert(string fileName)
        {
            return fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
        }

        public void Convert(string inputFile, string outputFile)
        {
            using var reader = new StreamReader(inputFile, EncodingHelper.DetectEncoding(inputFile));
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                BadDataFound = null,
                MissingFieldFound = null
            });

            var records = csv.GetRecords<dynamic>().ToList();
            string json = JsonConvert.SerializeObject(records, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(outputFile, json, Encoding.UTF8);
            Log.Information("Archivo CSV convertido: {FileName} a JSON.", Path.GetFileName(inputFile));
        }
    }
}
