using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CsvHelper;
using System.Globalization;
using Microsoft.Data.Sqlite;
using System.Linq;

public class Setup
{
    public static void Main(string[] args)
    {
        string dbPath = "Data Source=databases/iei2.db;";
        using var conn = new SqliteConnection(dbPath);
        conn.Open();

        //Transformations.ConvertFolderToJson("info");

        /*CreateTables(conn);
        InsertJsonData(conn, "info/estaciones.json");
        InsertXmlData(conn, "info/ITV-CAT.xml");
        InsertCsvData(conn, "info/Estacions_ITV.csv");*/
    }

    static void CreateTables(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            DROP TABLE IF EXISTS ESTACIONES_JSON;
            DROP TABLE IF EXISTS ESTACIONES_XML;
            DROP TABLE IF EXISTS ESTACIONES_CSV;
        ";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ESTACIONES_JSON (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                TIPO_ESTACION TEXT,
                PROVINCIA TEXT,
                MUNICIPIO TEXT,
                CODIGO_POSTAL TEXT,
                DIRECCION TEXT,
                NUMERO_ESTACION TEXT,
                HORARIO TEXT,
                CORREO TEXT
            );";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ESTACIONES_XML (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                ESTACION TEXT,
                DENOMINACION TEXT,
                OPERADOR TEXT,
                DIRECCION TEXT,
                CODIGO_POSTAL TEXT,
                MUNICIPIO TEXT,
                CODIGO_MUNICIPIO TEXT,
                LATITUD TEXT,
                LONGITUD TEXT,
                COLUMNA_GEOCODIFICADA TEXT,
                LOCALIZADOR_GOOGLE_MAPS TEXT,
                SERVICIOS_TERRITORIALES TEXT,
                HORARIO_SERVICIO TEXT,
                EMAIL TEXT,
                WEB TEXT
            );";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ESTACIONES_CSV (
                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                ESTACION TEXT,
                ENDEREZO TEXT,
                CONCELLO TEXT,
                CODIGO_POSTAL TEXT,
                PROVINCIA TEXT,
                TELEFONO TEXT,
                HORARIO TEXT,
                SOLICITUD_CITA_PREVIA TEXT,
                EMAIL TEXT,
                COORDENADAS_GOOGLE_MAPS TEXT,
                LATITUD TEXT,
                LONGITUD TEXT
            );";
        cmd.ExecuteNonQuery();
    }

    static void InsertJsonData(SqliteConnection conn, string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"JSON file not found: {filePath}");
            return;
        }

        try
        {
            string jsonContent = File.ReadAllText(filePath);
            var jsonData = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ESTACIONES_JSON 
                (TIPO_ESTACION, PROVINCIA, MUNICIPIO, CODIGO_POSTAL, DIRECCION, NUMERO_ESTACION, HORARIO, CORREO)
                VALUES (@tipo, @provincia, @municipio, @codigo, @direccion, @numero, @horario, @correo)";

            cmd.Parameters.AddWithValue("@tipo", "");
            cmd.Parameters.AddWithValue("@provincia", "");
            cmd.Parameters.AddWithValue("@municipio", "");
            cmd.Parameters.AddWithValue("@codigo", "");
            cmd.Parameters.AddWithValue("@direccion", "");
            cmd.Parameters.AddWithValue("@numero", "");
            cmd.Parameters.AddWithValue("@horario", "");
            cmd.Parameters.AddWithValue("@correo", "");

            int recordsInserted = 0;

            // map correct field names from the JSON
            if (jsonData.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in jsonData.EnumerateArray())
                {
                    cmd.Parameters["@tipo"].Value = item.TryGetProperty("TIPO ESTACIÓN", out var tipo) ? tipo.GetString() : "";
                    cmd.Parameters["@provincia"].Value = item.TryGetProperty("PROVINCIA", out var prov) ? prov.GetString() : "";
                    cmd.Parameters["@municipio"].Value = item.TryGetProperty("MUNICIPIO", out var mun) ? mun.GetString() : "";
                    cmd.Parameters["@codigo"].Value = item.TryGetProperty("C.POSTAL", out var cod) ? cod.ToString() : "";
                    cmd.Parameters["@direccion"].Value = item.TryGetProperty("DIRECCIÓN", out var dir) ? dir.GetString() : "";
                    cmd.Parameters["@numero"].Value = item.TryGetProperty("Nº ESTACIÓN", out var num) ? num.ToString() : "";
                    cmd.Parameters["@horario"].Value = item.TryGetProperty("HORARIOS", out var hor) ? hor.GetString() : "";
                    cmd.Parameters["@correo"].Value = item.TryGetProperty("CORREO", out var cor) ? cor.GetString() : "";

                    cmd.ExecuteNonQuery();
                    recordsInserted++;
                }
            }

            Console.WriteLine($"Successfully inserted {recordsInserted} records into ESTACIONES_JSON");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing JSON: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static void InsertXmlData(SqliteConnection conn, string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"XML file not found: {filePath}");
            return;
        }

        try
        {
            var doc = XDocument.Load(filePath);

            // target nested row elements inside the outer container row
            var elements = doc.Descendants("row").Where(r => r.Attribute("_id") != null).ToList();

            Console.WriteLine($"Found {elements.Count} station records in XML");

            if (elements.Count == 0)
            {
                Console.WriteLine("Could not find any suitable elements in XML");
                return;
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ESTACIONES_XML 
                (ESTACION, DENOMINACION, OPERADOR, DIRECCION, CODIGO_POSTAL, MUNICIPIO, CODIGO_MUNICIPIO, 
                 LATITUD, LONGITUD, COLUMNA_GEOCODIFICADA, LOCALIZADOR_GOOGLE_MAPS, SERVICIOS_TERRITORIALES, 
                 HORARIO_SERVICIO, EMAIL, WEB)
                VALUES (@estacion, @denominacion, @operador, @direccion, @codigo, @municipio, @codigoMun,
                        @latitud, @longitud, @columna, @localizador, @servicios, @horario, @email, @web)";

            cmd.Parameters.AddWithValue("@estacion", "");
            cmd.Parameters.AddWithValue("@denominacion", "");
            cmd.Parameters.AddWithValue("@operador", "");
            cmd.Parameters.AddWithValue("@direccion", "");
            cmd.Parameters.AddWithValue("@codigo", "");
            cmd.Parameters.AddWithValue("@municipio", "");
            cmd.Parameters.AddWithValue("@codigoMun", "");
            cmd.Parameters.AddWithValue("@latitud", "");
            cmd.Parameters.AddWithValue("@longitud", "");
            cmd.Parameters.AddWithValue("@columna", "");
            cmd.Parameters.AddWithValue("@localizador", "");
            cmd.Parameters.AddWithValue("@servicios", "");
            cmd.Parameters.AddWithValue("@horario", "");
            cmd.Parameters.AddWithValue("@email", "");
            cmd.Parameters.AddWithValue("@web", "");

            int recordsInserted = 0;

            // map to correct field names in the XML
            foreach (var element in elements)
            {
                cmd.Parameters["@estacion"].Value = element.Element("estaci")?.Value ?? "";
                cmd.Parameters["@denominacion"].Value = element.Element("denominaci")?.Value ?? "";
                cmd.Parameters["@operador"].Value = element.Element("operador")?.Value ?? "";
                cmd.Parameters["@direccion"].Value = element.Element("adre_a")?.Value ?? "";
                cmd.Parameters["@codigo"].Value = element.Element("cp")?.Value ?? "";
                cmd.Parameters["@municipio"].Value = element.Element("municipi")?.Value ?? "";
                cmd.Parameters["@codigoMun"].Value = element.Element("codi_municipi")?.Value ?? "";
                cmd.Parameters["@latitud"].Value = element.Element("lat")?.Value ?? "";
                cmd.Parameters["@longitud"].Value = element.Element("long")?.Value ?? "";
                cmd.Parameters["@columna"].Value = element.Element("geocoded_column")?.Value ?? "";
                cmd.Parameters["@localizador"].Value = element.Element("localitzador_a_google_maps")?.Attribute("url")?.Value ?? "";
                cmd.Parameters["@servicios"].Value = element.Element("serveis_territorials")?.Value ?? "";
                cmd.Parameters["@horario"].Value = element.Element("horari_de_servei")?.Value ?? "";
                cmd.Parameters["@email"].Value = element.Element("correu_electr_nic")?.Value ?? "";
                cmd.Parameters["@web"].Value = element.Element("web")?.Attribute("url")?.Value ?? "";

                cmd.ExecuteNonQuery();
                recordsInserted++;
            }

            Console.WriteLine($"Successfully inserted {recordsInserted} records into ESTACIONES_XML");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing XML: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static void InsertCsvData(SqliteConnection conn, string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"CSV file not found: {filePath}");
            return;
        }

        try
        {
            // handle semicolon separated format
            string[] lines = File.ReadAllLines(filePath, Encoding.GetEncoding("ISO-8859-1")); // encoding for accents

            if (lines.Length == 0)
            {
                Console.WriteLine("CSV file is empty");
                return;
            }

            // get column headers
            string[] headers = lines[0].Split(';');
            Console.WriteLine($"CSV has {headers.Length} columns: {string.Join(", ", headers)}");

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ESTACIONES_CSV 
                (ESTACION, ENDEREZO, CONCELLO, CODIGO_POSTAL, PROVINCIA, TELEFONO, HORARIO, 
                 SOLICITUD_CITA_PREVIA, EMAIL, COORDENADAS_GOOGLE_MAPS, LATITUD, LONGITUD)
                VALUES (@estacion, @enderezo, @concello, @codigo, @provincia, @telefono, @horario,
                        @cita, @email, @coordenadas, @latitud, @longitud)";

            cmd.Parameters.AddWithValue("@estacion", "");
            cmd.Parameters.AddWithValue("@enderezo", "");
            cmd.Parameters.AddWithValue("@concello", "");
            cmd.Parameters.AddWithValue("@codigo", "");
            cmd.Parameters.AddWithValue("@provincia", "");
            cmd.Parameters.AddWithValue("@telefono", "");
            cmd.Parameters.AddWithValue("@horario", "");
            cmd.Parameters.AddWithValue("@cita", "");
            cmd.Parameters.AddWithValue("@email", "");
            cmd.Parameters.AddWithValue("@coordenadas", "");
            cmd.Parameters.AddWithValue("@latitud", "");
            cmd.Parameters.AddWithValue("@longitud", "");

            int recordsInserted = 0;

            // skip the header while processing lines
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                // semicolon split
                string[] fields = lines[i].Split(';');

                // map fields to columns using direct indexing
                cmd.Parameters["@estacion"].Value = fields.Length > 0 ? fields[0] : "";
                cmd.Parameters["@enderezo"].Value = fields.Length > 1 ? fields[1] : "";
                cmd.Parameters["@concello"].Value = fields.Length > 2 ? fields[2] : "";
                cmd.Parameters["@codigo"].Value = fields.Length > 3 ? fields[3] : "";
                cmd.Parameters["@provincia"].Value = fields.Length > 4 ? fields[4] : "";
                cmd.Parameters["@telefono"].Value = fields.Length > 5 ? fields[5] : "";
                cmd.Parameters["@horario"].Value = fields.Length > 6 ? fields[6] : "";
                cmd.Parameters["@cita"].Value = fields.Length > 7 ? fields[7] : "";
                cmd.Parameters["@email"].Value = fields.Length > 8 ? fields[8] : "";

                // store full string in coordinates
                string coords = fields.Length > 9 ? fields[9] : "";
                cmd.Parameters["@coordenadas"].Value = coords;

                // parse lat/long from the coordinates field if possible because of different formats
                if (!string.IsNullOrEmpty(coords))
                {
                    string[] parts = coords.Split(',');
                    cmd.Parameters["@latitud"].Value = parts.Length > 0 ? parts[0].Trim() : "";
                    cmd.Parameters["@longitud"].Value = parts.Length > 1 ? parts[1].Trim() : "";
                }
                else
                {
                    cmd.Parameters["@latitud"].Value = "";
                    cmd.Parameters["@longitud"].Value = "";
                }

                cmd.ExecuteNonQuery();
                recordsInserted++;
            }

            Console.WriteLine($"Successfully inserted {recordsInserted} records into ESTACIONES_CSV");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing CSV: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static string GetFieldByIndex(CsvReader csv, int index)
    {
        if (index < 0 || index >= csv.Parser.Count)
            return "";

        try
        {
            return csv.GetField(index) ?? "";
        }
        catch
        {
            return "";
        }
    }
}
