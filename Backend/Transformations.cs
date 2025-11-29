using Backend.Converters;
using Serilog;
using System.Text;

namespace Backend
{
    public class Transformations
    {
        private static readonly List<IFileConverter> _converters;

        static Transformations()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _converters = new List<IFileConverter>
            {
                new XmlToJSONConverter(),
                new CsvToJSONConverter(),
                new JsonToJSONConverter()
            };
        }

        public static void ConvertFolderToJson(string inputFolder, string outputFolder)
        {
            Log.Information("Empezando transformación de la carpeta: {InputFolder} a JSON en la carpeta: {OutputFolder}", inputFolder, outputFolder);

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
                Log.Information("Carpeta de salida creada: {OutputFolder}", outputFolder);
            }

            var files = Directory.GetFiles(inputFolder);
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string outputPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(file) + ".json");

                try
                {
                    Log.Information("Procesando archivo: {FileName}", fileName);

                    var converter = _converters.FirstOrDefault(c => c.CanConvert(fileName));

                    if (converter != null)
                    {
                        converter.Convert(file, outputPath);
                    }
                    else
                    {
                        Log.Warning("No se encontró convertidor para el archivo: {FileName}", fileName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error transformando archivo: {FileName}", fileName);
                }
            }

            Log.Information("Transformación completada para la carpeta: {InputFolder}", inputFolder);
        }
    }
}