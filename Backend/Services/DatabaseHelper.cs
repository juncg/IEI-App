using System.IO;

namespace Backend.Services
{
    public static class DatabaseHelper
    {
        public static string GetDatabasePath(string basePath)
        {
            return basePath;
        }

        public static string GetConnectionString(string dbPath)
        {
            return $"Data Source={dbPath}";
        }
    }
}
