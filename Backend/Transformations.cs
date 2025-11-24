using System.Text;
using System.Xml;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using System.Globalization;

namespace Backend
{
    public class Transformations
    {
        public static void ConvertFolderToJson(string inputFolder, string outputFolder)
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            var files = Directory.GetFiles(inputFolder);
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string outputPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(file) + ".json");

                try
                {
                    if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(file);
                        string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(outputPath, json);
                        Console.WriteLine($"Converted {fileName} to JSON.");
                    }
                    else if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        using var reader = new StreamReader(file, Encoding.GetEncoding("ISO-8859-1"));
                        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            Delimiter = ";",
                            BadDataFound = null,
                            MissingFieldFound = null
                        });

                        var records = csv.GetRecords<dynamic>().ToList();
                        string json = JsonConvert.SerializeObject(records, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(outputPath, json);
                        Console.WriteLine($"Converted {fileName} to JSON.");
                    }
                    else if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(file, outputPath, true);
                        Console.WriteLine($"Copied {fileName}.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error transforming {fileName}: {ex.Message}");
                }
            }
        }
    }
}