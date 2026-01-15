using Microsoft.Data.Sqlite;

namespace Backend.Services
{
    public static class DatabaseInitializer
    {
        /// <summary>
        /// Inicializa la base de datos creando las tablas necesarias si no existen
        /// </summary>
        /// <param name="conn">Conexión a la base de datos SQLite</param>
        /// <param name="dropIfExists">Si es true, elimina las tablas existentes antes de crearlas</param>
        public static void Initialize(SqliteConnection conn, bool dropIfExists = false)
        {
            using var cmd = conn.CreateCommand();

            if (dropIfExists)
            {
                cmd.CommandText = @"
                    DROP TABLE IF EXISTS Estacion;
                    DROP TABLE IF EXISTS Localidad;
                    DROP TABLE IF EXISTS Provincia;
                ";
                cmd.ExecuteNonQuery();
            }

            // Check if tables exist
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Estacion'";
            var exists = cmd.ExecuteScalar() != null;

            if (!exists)
            {
                cmd.CommandText = @"
                    CREATE TABLE Provincia (
                        codigo INTEGER PRIMARY KEY AUTOINCREMENT,
                        nombre TEXT NOT NULL UNIQUE
                    );
                    CREATE TABLE Localidad (
                        codigo INTEGER PRIMARY KEY AUTOINCREMENT,
                        nombre TEXT NOT NULL,
                        en_provincia INTEGER,
                        FOREIGN KEY(en_provincia) REFERENCES Provincia(codigo)
                    );
                    CREATE TABLE Estacion (
                        cod_estacion INTEGER PRIMARY KEY AUTOINCREMENT,
                        nombre TEXT,
                        tipo INTEGER,
                        direccion TEXT,
                        codigo_postal TEXT,
                        longitud REAL,
                        latitud REAL,
                        descripcion TEXT,
                        horario TEXT,
                        contacto TEXT,
                        URL TEXT,
                        en_localidad INTEGER,
                        FOREIGN KEY(en_localidad) REFERENCES Localidad(codigo)
                    );
                ";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
