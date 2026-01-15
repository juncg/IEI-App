using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Text;

namespace Backend.Repositories
{
    public class LocationRepository
    {

        /// <summary>
        /// Obtiene el ID de una provincia existente o inserta una nueva si no existe
        /// </summary>
        /// <param name="conn">Conexión a la base de datos SQLite</param>
        /// <param name="name">Nombre de la provincia</param>
        /// <param name="trans">Transacción de base de datos activa</param>
        /// <returns>ID de la provincia (existente o recién insertada)</returns>
        public int GetOrInsertProvince(SqliteConnection conn, string name, SqliteTransaction trans)
        {
            conn.CreateFunction("RemoveAccents", (string text) => Utilities.RemoveAccents(text ?? ""));

            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.Transaction = trans;
                checkCmd.CommandText = "SELECT codigo FROM Provincia WHERE UPPER(RemoveAccents(TRIM(nombre))) = UPPER(RemoveAccents(TRIM(@nombre)))";
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

        /// <summary>
        /// Obtiene el ID de una localidad existente o inserta una nueva si no existe
        /// </summary>
        /// <param name="conn">Conexión a la base de datos SQLite</param>
        /// <param name="name">Nombre de la localidad</param>
        /// <param name="provinceId">ID de la provincia a la que pertenece la localidad</param>
        /// <param name="trans">Transacción de base de datos activa</param>
        /// <returns>ID de la localidad (existente o recién insertada)</returns>
        public int GetOrInsertLocality(SqliteConnection conn, string name, int provinceId, SqliteTransaction trans)
        {
            conn.CreateFunction("RemoveAccents", (string text) => Utilities.RemoveAccents(text ?? ""));

            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.Transaction = trans;
                checkCmd.CommandText = "SELECT codigo FROM Localidad WHERE UPPER(RemoveAccents(TRIM(nombre))) = UPPER(RemoveAccents(TRIM(@nombre))) AND en_provincia = @provId";
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
