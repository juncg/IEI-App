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
using Backend.Inserter;

public class Transformations
{
    public static void Main(string[] args)
    {
        // Ejemplo de uso
        ConvertToJson("info/archivo.csv");
        ConvertToJson("info/archivo.xml");

        // O convertir toda una carpeta
        ConvertFolderToJson("info");
    }

    /// <summary>
    /// Convierte un archivo (CSV o XML) a JSON automáticamente según su extensión
    /// </summary>
    public static void ConvertToJson(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Archivo no encontrado: {filePath}");
            return;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var jsonPath = Path.ChangeExtension(filePath, ".json");

        // Verificar si ya existe el JSON
        if (File.Exists(jsonPath))
        {
            Console.WriteLine($"JSON ya existe para {Path.GetFileName(filePath)} - omitiendo.");
            return;
        }

        try
        {
            List<Dictionary<string, object>> result;

            switch (extension)
            {
                case ".csv":
                    result = ConvertCsvToList(filePath);
                    break;
                case ".xml":
                    result = ConvertXmlToList(filePath);
                    break;
                default:
                    Console.WriteLine($"Formato no soportado: {extension}");
                    return;
            }

            // Serializar a JSON con codificación UTF-8
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(result, options);
            File.WriteAllText(jsonPath, json, new UTF8Encoding(true));

            Console.WriteLine($"✓ Convertido {extension.ToUpper()} -> JSON: {Path.GetFileName(filePath)} -> {Path.GetFileName(jsonPath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al convertir {filePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Convierte todos los archivos CSV y XML de una carpeta a JSON
    /// </summary>
    public static void ConvertFolderToJson(string folderPath = "info")
    {
        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"Carpeta no encontrada: {folderPath}");
            return;
        }

        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
                       f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Console.WriteLine($"Encontrados {files.Count} archivos para convertir en {folderPath}\n");

        foreach (var file in files)
        {
            ConvertToJson(file);
        }

        Console.WriteLine($"\n✓ Conversión completada. Procesados {files.Count} archivos.");
    }

    private static List<Dictionary<string, object>> ConvertCsvToList(string csvPath)
    {
        var lines = File.ReadAllLines(csvPath, Encoding.GetEncoding("ISO-8859-1"));
        var result = new List<Dictionary<string, object>>();

        if (lines.Length == 0)
            return result;

        var headers = lines[0].Split(';').Select(h => SanitizeHeader(h)).ToArray();

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            var fields = lines[i].Split(';');
            var obj = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            for (int j = 0; j < headers.Length; j++)
            {
                var key = headers[j];
                var val = j < fields.Length ? fields[j] : "";
                obj[key] = val;
            }

            result.Add(obj);
        }

        return result;
    }

    private static List<Dictionary<string, object>> ConvertXmlToList(string xmlPath)
    {
        var doc = XDocument.Load(xmlPath, LoadOptions.None);
        var result = new List<Dictionary<string, object>>();

        // Buscar elementos "row" repetidos; si no hay, usar elementos de primer nivel
        var rows = doc.Descendants("row").ToList();
        if (rows.Count == 0)
        {
            rows = doc.Root != null ? doc.Root.Elements().ToList() : new List<XElement>();
        }

        foreach (var row in rows)
        {
            var obj = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // Incluir atributos del row (prefijados con @)
            foreach (var attr in row.Attributes())
                obj[$"@{attr.Name.LocalName}"] = attr.Value;

            // Incluir elementos hijo
            foreach (var child in row.Elements())
            {
                if (child.HasElements)
                {
                    // Conversión simple: mapear nombres de elementos hijo a sus valores
                    var nested = new Dictionary<string, object>();
                    foreach (var sub in child.Elements())
                        nested[sub.Name.LocalName] = sub.HasElements
                            ? (object)sub.Elements().ToDictionary(e => e.Name.LocalName, e => (object)e.Value)
                            : sub.Value;

                    obj[child.Name.LocalName] = nested;
                }
                else
                {
                    // Incluir atributos del elemento, si los hay
                    if (child.HasAttributes)
                    {
                        var container = new Dictionary<string, object>();
                        foreach (var a in child.Attributes())
                            container[$"@{a.Name.LocalName}"] = a.Value;

                        if (!string.IsNullOrEmpty(child.Value))
                            container["#text"] = child.Value;

                        obj[child.Name.LocalName] = container;
                    }
                    else
                    {
                        obj[child.Name.LocalName] = child.Value;
                    }
                }
            }

            result.Add(obj);
        }

        return result;
    }

    static string SanitizeHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return "_unknown";

        var s = header.Trim();
        s = s.Trim('\uFEFF', '\u200B');
        s = string.Join("_", s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        return s;
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
            string jsonContent = File.ReadAllText(filePath, Encoding.UTF8);
            var jsonData = JsonSerializer.Deserialize<JsonElement>(jsonContent);

            int recordsInserted = 0;

            if (jsonData.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in jsonData.EnumerateArray())
                {
                    string province = item.TryGetProperty("PROVINCIA", out var prov) ? prov.GetString() : "";
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
            string[] lines = File.ReadAllLines(filePath, Encoding.GetEncoding("ISO-8859-1"));

            if (lines.Length == 0)
            {
                Console.WriteLine("CSV file is empty");
                return;
            }

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

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                string[] fields = lines[i].Split(';');

                cmd.Parameters["@estacion"].Value = fields.Length > 0 ? fields[0] : "";
                cmd.Parameters["@enderezo"].Value = fields.Length > 1 ? fields[1] : "";
                cmd.Parameters["@concello"].Value = fields.Length > 2 ? fields[2] : "";
                cmd.Parameters["@codigo"].Value = fields.Length > 3 ? fields[3] : "";
                cmd.Parameters["@provincia"].Value = fields.Length > 4 ? fields[4] : "";
                cmd.Parameters["@telefono"].Value = fields.Length > 5 ? fields[5] : "";
                cmd.Parameters["@horario"].Value = fields.Length > 6 ? fields[6] : "";
                cmd.Parameters["@cita"].Value = fields.Length > 7 ? fields[7] : "";
                cmd.Parameters["@email"].Value = fields.Length > 8 ? fields[8] : "";

                string coords = fields.Length > 9 ? fields[9] : "";
                cmd.Parameters["@coordenadas"].Value = coords;

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
