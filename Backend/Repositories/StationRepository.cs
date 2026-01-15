 using Backend.Models;
 using Microsoft.Data.Sqlite;
 using System.Globalization;
 using System.Text;

 namespace Backend.Repositories
 {
     public class StationRepository
     {
         private static string RemoveAccents(string text)
         {
             if (string.IsNullOrEmpty(text)) return text;
             var normalizedString = text.Normalize(NormalizationForm.FormD);
             var stringBuilder = new StringBuilder();
             foreach (var c in normalizedString)
             {
                 var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                 if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                 {
                     stringBuilder.Append(c);
                 }
             }
             return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
         }
        public string InsertStation(SqliteConnection conn, Station s, int? localityId, SqliteTransaction trans)
        {
            conn.CreateFunction("RemoveAccents", (string text) => RemoveAccents(text ?? ""));

            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.Transaction = trans;
                checkCmd.CommandText = "SELECT COUNT(*) FROM Estacion WHERE UPPER(RemoveAccents(TRIM(nombre))) = UPPER(RemoveAccents(TRIM(@nombre))) AND tipo = @tipo";
                checkCmd.Parameters.AddWithValue("@nombre", s.name ?? "");
                checkCmd.Parameters.AddWithValue("@tipo", (int)s.type);
                var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    return "duplicated";
                }
                else
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
                    cmd.Parameters.AddWithValue("@locId", localityId.HasValue ? localityId.Value : (object)DBNull.Value);

                    cmd.ExecuteNonQuery();
                    return "inserted";
                }

            }
        }

        public List<Station> SearchStations(SqliteConnection conn, string? name, int? type, string? locality, string? postalCode, string? province)
        {
            var results = new List<Station>();
            using var cmd = conn.CreateCommand();

            var query = @"
                SELECT e.cod_estacion, e.nombre, e.tipo, e.direccion, e.codigo_postal, e.longitud, e.latitud, e.descripcion, e.horario, e.contacto, e.URL, l.nombre as localidad, p.nombre as provincia
                FROM Estacion e
                LEFT JOIN Localidad l ON e.en_localidad = l.codigo
                LEFT JOIN Provincia p ON l.en_provincia = p.codigo
                WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(name))
            {
                query += " AND e.nombre LIKE @name";
                cmd.Parameters.AddWithValue("@name", $"%{name}%");
            }

            if (type.HasValue)
            {
                query += " AND e.tipo = @type";
                cmd.Parameters.AddWithValue("@type", type.Value);
            }

            if (!string.IsNullOrWhiteSpace(locality))
            {
                query += " AND l.nombre LIKE @locality";
                cmd.Parameters.AddWithValue("@locality", $"%{locality}%");
            }

            if (!string.IsNullOrWhiteSpace(postalCode))
            {
                query += " AND e.codigo_postal LIKE @postalCode";
                cmd.Parameters.AddWithValue("@postalCode", $"%{postalCode}%");
            }

            if (!string.IsNullOrWhiteSpace(province))
            {
                query += " AND p.nombre LIKE @province";
                cmd.Parameters.AddWithValue("@province", $"%{province}%");
            }

            cmd.CommandText = query;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Station station = new Station
                {
                    code = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                    name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    type = (StationType)(reader.IsDBNull(2) ? 0 : reader.GetInt32(2)),
                    address = reader.IsDBNull(3) ? null : reader.GetString(3),
                    postal_code = reader.IsDBNull(4) ? null : reader.GetString(4),
                    longitude = reader.IsDBNull(5) ? (double?)null : reader.GetDouble(5),
                    latitude = reader.IsDBNull(6) ? (double?)null : reader.GetDouble(6),
                    description = reader.IsDBNull(7) ? null : reader.GetString(7),
                    schedule = reader.IsDBNull(8) ? null : reader.GetString(8),
                    contact = reader.IsDBNull(9) ? null : reader.GetString(9),
                    url = reader.IsDBNull(10) ? null : reader.GetString(10),
                    locality = reader.IsDBNull(11) ? null : reader.GetString(11),
                    province = reader.IsDBNull(12) ? null : reader.GetString(12)
                };
                results.Add(station);
            }

            return results;
        }

        public List<Station> GetStationsWithCoordinates(SqliteConnection conn)
        {
            var results = new List<Station>();
            using var cmd = conn.CreateCommand();

            var query = @"
                SELECT e.cod_estacion, e.nombre, e.tipo, e.direccion, e.codigo_postal, e.longitud, e.latitud, e.descripcion, e.horario, e.contacto, e.URL, l.nombre as localidad, p.nombre as provincia
                FROM Estacion e
                LEFT JOIN Localidad l ON e.en_localidad = l.codigo
                LEFT JOIN Provincia p ON l.en_provincia = p.codigo
                WHERE e.longitud IS NOT NULL AND e.latitud IS NOT NULL";

            cmd.CommandText = query;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                Station station = new Station
                {
                    code = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                    name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    type = (StationType)(reader.IsDBNull(2) ? 0 : reader.GetInt32(2)),
                    address = reader.IsDBNull(3) ? null : reader.GetString(3),
                    postal_code = reader.IsDBNull(4) ? null : reader.GetString(4),
                    longitude = reader.IsDBNull(5) ? (double?)null : reader.GetDouble(5),
                    latitude = reader.IsDBNull(6) ? (double?)null : reader.GetDouble(6),
                    description = reader.IsDBNull(7) ? null : reader.GetString(7),
                    schedule = reader.IsDBNull(8) ? null : reader.GetString(8),
                    contact = reader.IsDBNull(9) ? null : reader.GetString(9),
                    url = reader.IsDBNull(10) ? null : reader.GetString(10),
                    locality = reader.IsDBNull(11) ? null : reader.GetString(11),
                    province = reader.IsDBNull(12) ? null : reader.GetString(12)
                };
                results.Add(station);
            }

            return results;
        }
    }
}
