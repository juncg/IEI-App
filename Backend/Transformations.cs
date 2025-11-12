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
        string dbPath = "Data Source=databases/iei.db;";
        using var conn = new SqliteConnection(dbPath);
        conn.Open();

        InsertJsonData(conn, args[0]);
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

            int recordsInserted = 0;

            // map correct field names from the JSON
            if (jsonData.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in jsonData.EnumerateArray())
                {
                    string province = item.TryGetProperty("PROVINCIA", out var prov) ? prov.GetString() : "";
                    // InsertProvince(province, conn);
                    // cmd.Parameters["@tipo"].Value = item.TryGetProperty("TIPO ESTACIÓN", out var tipo) ? tipo.GetString() : "";
                    // cmd.Parameters["@municipio"].Value = item.TryGetProperty("MUNICIPIO", out var mun) ? mun.GetString() : "";
                    // cmd.Parameters["@codigo"].Value = item.TryGetProperty("C.POSTAL", out var cod) ? cod.ToString() : "";
                    // cmd.Parameters["@direccion"].Value = item.TryGetProperty("DIRECCIÓN", out var dir) ? dir.GetString() : "";
                    // cmd.Parameters["@numero"].Value = item.TryGetProperty("Nº ESTACIÓN", out var num) ? num.ToString() : "";
                    // cmd.Parameters["@horario"].Value = item.TryGetProperty("HORARIOS", out var hor) ? hor.GetString() : "";
                    // cmd.Parameters["@correo"].Value = item.TryGetProperty("CORREO", out var cor) ? cor.GetString() : "";

                    // cmd.ExecuteNonQuery();
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

    static void Mapper(SqliteConnection conn, string filePath)
    {
        if (!Directory.Exists(filePath))
        {
            Console.WriteLine($"Directory not found: {filePath}");
            return;
        }

        try
        {
            var jsonFiles = Directory.GetFiles(filePath, "*.json", SearchOption.TopDirectoryOnly);
            int stationCounter = 1;
            int locationCounter = 1;
            int provinceCounter = 1;

            foreach (var jsonFile in jsonFiles)
            {
                string fileName = Path.GetFileName(jsonFile).ToLower();
                string jsonContent = File.ReadAllText(jsonFile);
                var jsonData = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                var mappedData = new List<object>();

                if (jsonData.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in jsonData.EnumerateArray())
                    {
                        object mappedItem = null;

                        // Detect source type based on field names
                        if (item.TryGetProperty("PROVINCIA", out _) && item.TryGetProperty("Nº ESTACIÓN", out _))
                        {
                            // CV (Comunidad Valenciana) source
                            mappedItem = MapCVData(item, ref stationCounter, ref locationCounter, ref provinceCounter);
                        }
                        else if (item.TryGetProperty("NOME DA ESTACIÓN", out _) || item.TryGetProperty("COORDENADAS GMAPS", out _))
                        {
                            // GAL (Galicia) source
                            mappedItem = MapGALData(item, ref stationCounter, ref locationCounter, ref provinceCounter);
                        }
                        else if (item.TryGetProperty("ESTACION", out _) || item.TryGetProperty("DENOMINACION", out _))
                        {
                            // CAT (Catalunya) source
                            mappedItem = MapCATData(item, ref stationCounter, ref locationCounter, ref provinceCounter);
                        }

                        if (mappedItem != null)
                        {
                            mappedData.Add(mappedItem);
                        }
                    }
                }

                // Write mapped data to output directory
                string outputDir = Path.Combine(filePath, "mapped");
                Directory.CreateDirectory(outputDir);
                string outputFile = Path.Combine(outputDir, $"mapped_{Path.GetFileName(jsonFile)}");

                var options = new JsonSerializerOptions { WriteIndented = true };
                string mappedJson = JsonSerializer.Serialize(mappedData, options);
                File.WriteAllText(outputFile, mappedJson);
            }

            Console.WriteLine($"Successfully mapped {jsonFiles.Length} JSON files.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing directory: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static object MapCVData(JsonElement item, ref int stationCounter, ref int locationCounter, ref int provinceCounter)
    {
        string tipoEstacion = item.TryGetProperty("TIPO ESTACIÓN", out var tipo) ? tipo.GetString() : "";
        string municipio = item.TryGetProperty("MUNICIPIO", out var mun) ? mun.GetString() : "";
        string numeroEstacion = item.TryGetProperty("Nº ESTACIÓN", out var num) ? num.GetString() : "";
        
        string nombre = string.IsNullOrEmpty(municipio) ? 
            $"Estación ITV de {numeroEstacion}" : 
            $"Estación ITV de {municipio} {numeroEstacion}";

        return new
        {
            Estacion = new
            {
                cod_estacion = stationCounter++,
                nombre = nombre,
                tipo = MapTipoEstacion(tipoEstacion),
                direccion = item.TryGetProperty("DIRECCIÓN", out var dir) ? dir.GetString() : "",
                codigo_postal = item.TryGetProperty("C.POSTAL", out var cp) ? cp.GetString() : "",
                longitud = "", // TODO: Geocode from address
                latitud = "", // TODO: Geocode from address
                descripcion = "",
                horario = item.TryGetProperty("HORARIOS", out var hor) ? hor.GetString() : "",
                contacto = item.TryGetProperty("CORREO", out var cor) ? cor.GetString() : "",
                url = "https://sitval.com"
            },
            Localidad = new
            {
                codigo = locationCounter++,
                nombre = municipio
            },
            Provincia = new
            {
                codigo = provinceCounter++,
                nombre = item.TryGetProperty("PROVINCIA", out var prov) ? prov.GetString() : ""
            }
        };
    }

    static object MapGALData(JsonElement item, ref int stationCounter, ref int locationCounter, ref int provinceCounter)
    {
        string nombreEstacion = item.TryGetProperty("NOME DA ESTACIÓN", out var nom) ? nom.GetString() : "";
        string concello = item.TryGetProperty("CONCELLO", out var con) ? con.GetString() : "";
        
        string nombre = string.IsNullOrEmpty(nombreEstacion) ? 
            $"Estación ITV de {concello}" : 
            $"Estación ITV de {nombreEstacion}";

        // Parse coordinates: "42º 52' 47'' N 8º 32' 44'' O"
        string coordenadas = item.TryGetProperty("COORDENADAS GMAPS", out var coord) ? coord.GetString() : "";
        var (latitud, longitud) = ParseGaliciaCoordinates(coordenadas);

        string correo = item.TryGetProperty("CORREO ELECTRÓNICO", out var email) ? email.GetString() : "";
        string telefono = item.TryGetProperty("TELÉFONO", out var tel) ? tel.GetString() : "";
        string contacto = string.IsNullOrEmpty(correo) ? telefono : 
                          string.IsNullOrEmpty(telefono) ? correo : 
                          $"{correo}, {telefono}";

        return new
        {
            Estacion = new
            {
                cod_estacion = stationCounter++,
                nombre = nombre,
                tipo = "Estación_fija",
                direccion = item.TryGetProperty("ENDEREZO", out var end) ? end.GetString() : "",
                codigo_postal = item.TryGetProperty("CÓDIGO POSTAL", out var cp) ? cp.GetString() : "",
                longitud = longitud,
                latitud = latitud,
                descripcion = "",
                horario = item.TryGetProperty("HORARIO", out var hor) ? hor.GetString() : "",
                contacto = contacto,
                url = item.TryGetProperty("SOLICITUDE DE CITA PREVIA", out var url) ? url.GetString() : ""
            },
            Localidad = new
            {
                codigo = locationCounter++,
                nombre = concello
            },
            Provincia = new
            {
                codigo = provinceCounter++,
                nombre = item.TryGetProperty("PROVINCIA", out var prov) ? prov.GetString() : ""
            }
        };
    }

    static object MapCATData(JsonElement item, ref int stationCounter, ref int locationCounter, ref int provinceCounter)
    {
        string denominacion = item.TryGetProperty("DENOMINACION", out var denom) ? denom.GetString() : "";
        string municipio = item.TryGetProperty("MUNICIPIO", out var mun) ? mun.GetString() : "";
        string estacionId = item.TryGetProperty("ESTACION", out var est) ? est.GetString() : "";
        
        string nombre = string.IsNullOrEmpty(denominacion) ? 
            $"Estación ITV de {municipio} {estacionId}" : 
            $"Estación ITV de {denominacion} {estacionId}";

        // Parse coordinates (divide by 100000)
        double longitud = 0, latitud = 0;
        if (item.TryGetProperty("LONGITUD", out var lon) && lon.TryGetInt64(out long lonValue))
        {
            longitud = lonValue / 100000.0;
        }
        if (item.TryGetProperty("LATITUD", out var lat) && lat.TryGetInt64(out long latValue))
        {
            latitud = latValue / 100000.0;
        }

        // Check if SERVICIOS TERRITORIALES is a valid province
        string serviciosTerritoriales = item.TryGetProperty("SERVICIOS TERRITORIALES", out var st) ? st.GetString() : "";
        string provinciaNombre = IsValidProvince(serviciosTerritoriales) ? serviciosTerritoriales : "";

        return new
        {
            Estacion = new
            {
                cod_estacion = stationCounter++,
                nombre = nombre,
                tipo = "Estación_fija",
                direccion = item.TryGetProperty("DIRECCION", out var dir) ? dir.GetString() : "",
                codigo_postal = item.TryGetProperty("CODIGO POSTAL", out var cp) ? cp.GetString() : "",
                longitud = longitud.ToString(CultureInfo.InvariantCulture),
                latitud = latitud.ToString(CultureInfo.InvariantCulture),
                descripcion = "",
                horario = item.TryGetProperty("HORARIO", out var hor) ? hor.GetString() : "",
                contacto = item.TryGetProperty("EMAIL", out var email) ? email.GetString() : "",
                url = item.TryGetProperty("WEB", out var web) ? web.GetString() : ""
            },
            Localidad = new
            {
                codigo = item.TryGetProperty("CODIGO MUNICIPIO", out var codMun) ? codMun.GetString() : locationCounter++.ToString(),
                nombre = municipio
            },
            Provincia = new
            {
                codigo = provinceCounter++,
                nombre = provinciaNombre
            }
        };
    }

    static string MapTipoEstacion(string tipoOriginal)
    {
        if (string.IsNullOrEmpty(tipoOriginal)) return "Estación_fija";
        
        string tipoLower = tipoOriginal.ToLower();
        if (tipoLower.Contains("fija")) return "Estación_fija";
        if (tipoLower.Contains("movil") || tipoLower.Contains("móvil")) return "Estación_movil";
        return "Otros";
    }

    static (string latitud, string longitud) ParseGaliciaCoordinates(string coordenadas)
    {
        // Example: "42º 52' 47'' N 8º 32' 44'' O"
        // Formula: degrees + (minutes / 60)
        if (string.IsNullOrEmpty(coordenadas)) return ("", "");

        try
        {
            var parts = coordenadas.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Parse latitude (first coordinate with N/S)
            double latDeg = 0, latMin = 0;
            double lonDeg = 0, lonMin = 0;
            
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Contains("º"))
                {
                    if (i + 2 < parts.Length && (parts[i + 2].Contains("N") || parts[i + 2].Contains("S")))
                    {
                        latDeg = double.Parse(parts[i].Replace("º", ""), CultureInfo.InvariantCulture);
                        latMin = double.Parse(parts[i + 1].Replace("'", ""), CultureInfo.InvariantCulture);
                    }
                    else if (i + 2 < parts.Length && (parts[i + 2].Contains("O") || parts[i + 2].Contains("E")))
                    {
                        lonDeg = double.Parse(parts[i].Replace("º", ""), CultureInfo.InvariantCulture);
                        lonMin = double.Parse(parts[i + 1].Replace("'", ""), CultureInfo.InvariantCulture);
                    }
                }
            }
            
            double lat = latDeg + (latMin / 60.0);
            double lon = lonDeg + (lonMin / 60.0);
            
            // Western longitude is negative
            if (coordenadas.Contains("O")) lon = -lon;
            
            return (lat.ToString(CultureInfo.InvariantCulture), lon.ToString(CultureInfo.InvariantCulture));
        }
        catch
        {
            return ("", "");
        }
    }

    static bool IsValidProvince(string name)
    {
        // Check if the name is a known Spanish province
        var provinces = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Barcelona", "Girona", "Lleida", "Tarragona", "A Coruña", "Lugo", "Ourense", "Pontevedra",
            "Alicante", "Castellón", "Valencia", "Madrid", "Sevilla", "Málaga", "Cádiz", "Córdoba"
            // Add more provinces as needed
        };
        
        return provinces.Contains(name);
    }
}
