using Backend.Models;
using Microsoft.Data.Sqlite;

namespace Backend
{
    public class Inserter
    {
        private const string ConnectionString = "Data Source=databases/iei2.db";

        public static void Run(List<UnifiedData> data)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            InitializeDatabase(conn);

            var provinceCache = new Dictionary<string, int>();
            var localityCache = new Dictionary<string, int>();

            using var transaction = conn.BeginTransaction();
            try
            {
                foreach (var item in data)
                {
                    string provName = string.IsNullOrWhiteSpace(item.ProvinceName) ? "Desconocida" : item.ProvinceName.Trim();
                    if (!provinceCache.TryGetValue(provName, out int provinceId))
                    {
                        provinceId = InsertProvince(conn, provName, transaction);
                        provinceCache[provName] = provinceId;
                    }

                    string locName = string.IsNullOrWhiteSpace(item.LocalityName) ? "Desconocida" : item.LocalityName.Trim();
                    string locKey = $"{provinceId}-{locName}";
                    if (!localityCache.TryGetValue(locKey, out int localityId))
                    {
                        localityId = InsertLocality(conn, locName, provinceId, transaction);
                        localityCache[locKey] = localityId;
                    }

                    InsertStation(conn, item.Station, localityId, transaction);
                }

                transaction.Commit();
                Console.WriteLine("Database population completed successfully.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error inserting data: {ex.Message}");
            }
        }

        private static void InitializeDatabase(SqliteConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Province (
                    code INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE
                );
                CREATE TABLE IF NOT EXISTS Locality (
                    code INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    province_code INTEGER,
                    FOREIGN KEY(province_code) REFERENCES Province(code)
                );
                CREATE TABLE IF NOT EXISTS Station (
                    code INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT,
                    type INTEGER,
                    address TEXT,
                    postal_code TEXT,
                    longitude REAL,
                    latitude REAL,
                    description TEXT,
                    schedule TEXT,
                    contact TEXT,
                    url TEXT,
                    locality_code INTEGER,
                    FOREIGN KEY(locality_code) REFERENCES Locality(code)
                );
            ";
            cmd.ExecuteNonQuery();
        }

        private static int InsertProvince(SqliteConnection conn, string name, SqliteTransaction trans)
        {
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.Transaction = trans;
                checkCmd.CommandText = "SELECT code FROM Province WHERE name = @name";
                checkCmd.Parameters.AddWithValue("@name", name);
                var result = checkCmd.ExecuteScalar();
                if (result != null) return Convert.ToInt32(result);
            }

            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = "INSERT INTO Province (name) VALUES (@name); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@name", name);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static int InsertLocality(SqliteConnection conn, string name, int provinceId, SqliteTransaction trans)
        {
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.Transaction = trans;
                checkCmd.CommandText = "SELECT code FROM Locality WHERE name = @name AND province_code = @provId";
                checkCmd.Parameters.AddWithValue("@name", name);
                checkCmd.Parameters.AddWithValue("@provId", provinceId);
                var result = checkCmd.ExecuteScalar();
                if (result != null) return Convert.ToInt32(result);
            }

            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = "INSERT INTO Locality (name, province_code) VALUES (@name, @provId); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@provId", provinceId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static void InsertStation(SqliteConnection conn, Station s, int localityId, SqliteTransaction trans)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = trans;
            cmd.CommandText = @"
                INSERT INTO Station (name, type, address, postal_code, longitude, latitude, description, schedule, contact, url, locality_code)
                VALUES (@name, @type, @address, @postal, @lon, @lat, @desc, @schedule, @contact, @url, @locId)";
            
            cmd.Parameters.AddWithValue("@name", s.name ?? "");
            cmd.Parameters.AddWithValue("@type", (int)s.type);
            cmd.Parameters.AddWithValue("@address", s.address ?? "");
            cmd.Parameters.AddWithValue("@postal", s.postal_code ?? "");
            cmd.Parameters.AddWithValue("@lon", s.longitude ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@lat", s.latitude ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@desc", s.description ?? "");
            cmd.Parameters.AddWithValue("@schedule", s.schedule ?? "");
            cmd.Parameters.AddWithValue("@contact", s.contact ?? "");
            cmd.Parameters.AddWithValue("@url", s.url ?? "");
            cmd.Parameters.AddWithValue("@locId", localityId);
            
            cmd.ExecuteNonQuery();
        }
    }
}