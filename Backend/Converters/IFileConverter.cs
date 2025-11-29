namespace Backend.Converters
{
    public interface IFileConverter
    {
        bool CanConvert(string fileName);
        void Convert(string inputFile, string outputFile);
    }
}
