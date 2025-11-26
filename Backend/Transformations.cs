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
        static Transformations()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

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
                        // Try UTF-8 first, then fallback to other encodings
                        string xmlContent = TryReadWithEncodings(file);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmlContent);
                        string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(outputPath, json, Encoding.UTF8);
                        Console.WriteLine($"Converted {fileName} to JSON.");
                    }
                    else if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        using var reader = new StreamReader(file, DetectEncoding(file));
                        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            Delimiter = ";",
                            BadDataFound = null,
                            MissingFieldFound = null
                        });

                        var records = csv.GetRecords<dynamic>().ToList();
                        string json = JsonConvert.SerializeObject(records, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(outputPath, json, Encoding.UTF8);
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

        private static Encoding DetectEncoding(string file)
        {
            // Read more bytes for better detection
            byte[] buffer = new byte[1024];
            int bytesRead;

            using (var fileStream = File.OpenRead(file))
            {
                bytesRead = fileStream.Read(buffer, 0, buffer.Length);
            }

            if (bytesRead >= 3 && buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
                return Encoding.UTF8;
            if (bytesRead >= 2 && buffer[0] == 0xff && buffer[1] == 0xfe)
                return Encoding.Unicode;
            if (bytesRead >= 2 && buffer[0] == 0xfe && buffer[1] == 0xff)
                return Encoding.BigEndianUnicode;

            try
            {
                var utf8 = Encoding.UTF8;
                var decoder = utf8.GetDecoder();
                char[] chars = new char[bytesRead];
                decoder.Convert(buffer, 0, bytesRead, chars, 0, chars.Length, true, out _, out int charsUsed, out _);

                string sample = new string(chars, 0, charsUsed);
                if (sample.Contains('á') || sample.Contains('é') || sample.Contains('í') ||
                    sample.Contains('ó') || sample.Contains('ú') || sample.Contains('ñ') ||
                    sample.Contains('Á') || sample.Contains('É') || sample.Contains('Í') ||
                    sample.Contains('Ó') || sample.Contains('Ú') || sample.Contains('Ñ'))
                {
                    return Encoding.UTF8;
                }
            }
            catch
            {
                // Ignore decoding errors
            }

            return Encoding.GetEncoding("Windows-1252");
        }

        private static string TryReadWithEncodings(string file)
        {
            Encoding[] encodingsToTry = new[]
            {
                Encoding.UTF8,
                Encoding.GetEncoding("ISO-8859-1"),
                Encoding.GetEncoding("Windows-1252"),
                Encoding.Default
            };

            foreach (var encoding in encodingsToTry)
            {
                try
                {
                    return File.ReadAllText(file, encoding);
                }
                catch
                {
                    continue;
                }
            }

            return File.ReadAllText(file);
        }
    }
}