using Microsoft.Data.Sqlite;

namespace Backend.Services
{
    public static class DatabaseInitializer
    {
        public static void Initialize(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Provincia (
                    codigo INTEGER PRIMARY KEY AUTOINCREMENT,
                    nombre TEXT NOT NULL UNIQUE
                );
                CREATE TABLE IF NOT EXISTS Localidad (
                    codigo INTEGER PRIMARY KEY AUTOINCREMENT,
                    nombre TEXT NOT NULL,
                    en_provincia INTEGER,
                    FOREIGN KEY(en_provincia) REFERENCES Provincia(codigo)
                );
                CREATE TABLE IF NOT EXISTS Estacion (
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
