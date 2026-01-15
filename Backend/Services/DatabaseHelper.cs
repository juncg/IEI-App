using System.IO;

namespace Backend.Services
{
    public static class DatabaseHelper
    {
        /// <summary>
        /// Obtiene la ruta completa de la base de datos
        /// </summary>
        /// <param name="basePath">Ruta base del archivo de base de datos</param>
        /// <returns>Ruta completa de la base de datos</returns>
        public static string GetDatabasePath(string basePath)
        {
            return basePath;
        }

        /// <summary>
        /// Genera la cadena de conexión para SQLite
        /// </summary>
        /// <param name="dbPath">Ruta del archivo de base de datos</param>
        /// <returns>Cadena de conexión de SQLite</returns>
        public static string GetConnectionString(string dbPath)
        {
            return $"Data Source={dbPath}";
        }
    }
}
