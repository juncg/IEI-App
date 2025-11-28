using Backend.Models;
using Microsoft.Data.Sqlite;

namespace Backend.Repositories
{
    public class StationRepository
    {
        public void InsertStation(SqliteConnection conn, Station s, int localityId, SqliteTransaction trans)
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
