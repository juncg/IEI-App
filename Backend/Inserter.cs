using Backend.Models;
using Microsoft.Data.Sqlite;
using Serilog;

namespace Backend
{
    public class Inserter
    {
        private static string GetUniqueDatabasePath(string basePath)
        {
            // Corrección para manejar valores nulos
            string dir = Path.GetDirectoryName(basePath) ?? string.Empty;
            string file = Path.GetFileNameWithoutExtension(basePath) ?? string.Empty;
            string ext = Path.GetExtension(basePath) ?? string.Empty;

            string candidate = basePath;
            int counter = 1;
            while (File.Exists(candidate))
            {
                candidate = Path.Combine(dir, $"{file}{counter}{ext}");
                counter++;
            }
            return candidate;
        }

        private static readonly string ConnectionString = $"Data Source={GetUniqueDatabasePath("databases/iei.db")}";

        public static void Run(List<UnifiedData> data)
        {
            Log.Information("Starting database population. Total records: {RecordCount}", data.Count);
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            InitializeDatabase(conn);
            Log.Information("Database initialized.");

            var provinceCache = new Dictionary<string, int>();
            var localityCache = new Dictionary<string, int>();

            using var transaction = conn.BeginTransaction();
            try
            {
                foreach (var item in data)
                {
                    Log.Debug("Processing record: {@Record}", item);

                    string provName = string.IsNullOrWhiteSpace(item.ProvinceName) ? "Desconocida" : item.ProvinceName.Trim();
                    if (!provinceCache.TryGetValue(provName, out int provinceId))
                    {
                        provinceId = InsertProvince(conn, provName, transaction);
                        provinceCache[provName] = provinceId;
                        Log.Information("Inserted new province: {ProvinceName} with ID {ProvinceId}", provName, provinceId);
                    }

                    string locName = string.IsNullOrWhiteSpace(item.LocalityName) ? "Desconocida" : item.LocalityName.Trim();
                    string locKey = $"{provinceId}-{locName}";
                    if (!localityCache.TryGetValue(locKey, out int localityId))
                    {
                        localityId = InsertLocality(conn, locName, provinceId, transaction);
                        localityCache[locKey] = localityId;
                        Log.Information("Inserted new locality: {LocalityName} with ID {LocalityId}", locName, localityId);
                    }

                    InsertStation(conn, item.Station, localityId, transaction);
                    Log.Information("Inserted station: {StationName}", item.Station.name);
                }

                transaction.Commit();
                Log.Information("Database population completed successfully.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "Error inserting data.");
            }
        }

        private static void InitializeDatabase(SqliteConnection conn)
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

        private static int InsertProvince(SqliteConnection conn, string name, SqliteTransaction trans)
        {
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.Transaction = trans;
                checkCmd.CommandText = "SELECT codigo FROM Provincia WHERE nombre = @nombre";
                checkCmd.Parameters.AddWithValue("@nombre", name);
                var result = checkCmd.ExecuteScalar();
                if (result != null) return Convert.ToInt32(result);
            }

            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = "INSERT INTO Provincia (nombre) VALUES (@nombre); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@nombre", name);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static int InsertLocality(SqliteConnection conn, string name, int provinceId, SqliteTransaction trans)
        {
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.Transaction = trans;
                checkCmd.CommandText = "SELECT codigo FROM Localidad WHERE nombre = @nombre AND en_provincia = @provId";
                checkCmd.Parameters.AddWithValue("@nombre", name);
                checkCmd.Parameters.AddWithValue("@provId", provinceId);
                var result = checkCmd.ExecuteScalar();
                if (result != null) return Convert.ToInt32(result);
            }

            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = "INSERT INTO Localidad (nombre, en_provincia) VALUES (@nombre, @provId); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@nombre", name);
            cmd.Parameters.AddWithValue("@provId", provinceId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static void InsertStation(SqliteConnection conn, Station s, int localityId, SqliteTransaction trans)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = @"
                INSERT INTO Estacion (nombre, tipo, direccion, codigo_postal, longitud, latitud, descripcion, horario, contacto, URL, en_localidad)
                VALUES (@nombre, @tipo, @direccion, @postal, @lon, @lat, @desc, @horario, @contacto, @url, @locId)";

            cmd.Parameters.AddWithValue("@nombre", s.name ?? "");
            cmd.Parameters.AddWithValue("@tipo", (int)s.type);
            cmd.Parameters.AddWithValue("@direccion", s.address ?? "");
            cmd.Parameters.AddWithValue("@postal", s.postal_code ?? "");
            cmd.Parameters.AddWithValue("@lon", s.longitude ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@lat", s.latitude ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@desc", s.description ?? "");
            cmd.Parameters.AddWithValue("@horario", s.schedule ?? "");
            cmd.Parameters.AddWithValue("@contacto", s.contact ?? "");
            cmd.Parameters.AddWithValue("@url", s.url ?? "");
            cmd.Parameters.AddWithValue("@locId", localityId);

            cmd.ExecuteNonQuery();
        }
    }
}