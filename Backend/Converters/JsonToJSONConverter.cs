using Serilog;

namespace Backend.Converters
{
    public class JsonToJSONConverter : IFileConverter
    {
        public bool CanConvert(string fileName)
        {
            return fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
        }

        public void Convert(string inputFile, string outputFile)
        {
            File.Copy(inputFile, outputFile, true);
            Log.Information("Paso Converter: Archivo JSON copiado: {FileName}.", Path.GetFileName(inputFile));
        }
    }
}
