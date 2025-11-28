using Microsoft.Data.Sqlite;

namespace Backend.Repositories
{
    public class LocationRepository
    {
        public int GetOrInsertProvince(SqliteConnection conn, string name, SqliteTransaction trans)
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

        public int GetOrInsertLocality(SqliteConnection conn, string name, int provinceId, SqliteTransaction trans)
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
    }
}
