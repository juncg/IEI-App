using Backend.Models;
using Microsoft.Data.Sqlite;

namespace Backend.Repositories
{
    public class StationRepository
    {
        public void InsertStation(SqliteConnection conn, Station s, int? localityId, SqliteTransaction trans)
        {
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.Transaction = trans;
                checkCmd.CommandText = "SELECT COUNT(*) FROM Estacion WHERE nombre = @nombre AND tipo = @tipo";
                checkCmd.Parameters.AddWithValue("@nombre", s.name ?? "");
                checkCmd.Parameters.AddWithValue("@tipo", (int)s.type);
                checkCmd.Parameters.AddWithValue("@direccion", s.address ?? "");
                var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0) return;
            }

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
            cmd.Parameters.AddWithValue("@locId", localityId.HasValue ? localityId.Value : (object)DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public List<object> SearchStations(SqliteConnection conn, string? name, string? type, string? locality)
        {
            var results = new List<object>();
            using var cmd = conn.CreateCommand();
            
            var query = @"
                SELECT e.nombre, e.tipo, e.direccion, e.codigo_postal, e.longitud, e.latitud, l.nombre as localidad, p.nombre as provincia
                FROM Estacion e
                LEFT JOIN Localidad l ON e.en_localidad = l.codigo
                LEFT JOIN Provincia p ON l.en_provincia = p.codigo
                WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(name))
            {
                query += " AND e.nombre LIKE @name";
                cmd.Parameters.AddWithValue("@name", $"%{name}%");
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                // Assuming type is passed as string but stored as int enum
                // If passed as int string "1", "2", etc.
                if (int.TryParse(type, out int typeId))
                {
                    query += " AND e.tipo = @type";
                    cmd.Parameters.AddWithValue("@type", typeId);
                }
            }

            if (!string.IsNullOrWhiteSpace(locality))
            {
                query += " AND l.nombre LIKE @locality";
                cmd.Parameters.AddWithValue("@locality", $"%{locality}%");
            }

            cmd.CommandText = query;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new
                {
                    Name = reader.IsDBNull(0) ? null : reader.GetString(0),
                    Type = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    Address = reader.IsDBNull(2) ? null : reader.GetString(2),
                    PostalCode = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Longitude = reader.IsDBNull(4) ? (double?)null : reader.GetDouble(4),
                    Latitude = reader.IsDBNull(5) ? (double?)null : reader.GetDouble(5),
                    Locality = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Province = reader.IsDBNull(7) ? null : reader.GetString(7)
                });
            }

            return results;
        }
    }
}
