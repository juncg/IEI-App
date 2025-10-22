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

namespace Backend.Inserter;
public class Inserter
{
    public static int InsertProvince(string name, SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Provincia (nombre) VALUES (@name); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@name", name);
        long id = (long)cmd.ExecuteScalar();
        return (int)id;
    }

    public static int InsertLocality(string name, int provinceCode, SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Localidad (nombre, codigo_provincia) VALUES (@name, @province_code); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@province_code", provinceCode);
        long id = (long)cmd.ExecuteScalar();
        return (int)id;
    }

    static void TemporalMethod(SqliteConnection conn, string filePath)
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

            var provinceDict = new Dictionary<string, int>();
            var localityDict = new Dictionary<string, int>();

            int recordsInserted = 0;

            // map correct field names from the JSON
            if (jsonData.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in jsonData.EnumerateArray())
                {
                    string provincia = item.TryGetProperty("PROVINCIA", out var prov) ? prov.GetString() ?? "" : "";
                    string municipio = item.TryGetProperty("MUNICIPIO", out var mun) ? mun.GetString() ?? "" : "";

                    if (string.IsNullOrWhiteSpace(provincia) || string.IsNullOrWhiteSpace(municipio))
                        continue;

                    if (!provinceDict.ContainsKey(provincia))
                    {
                        int provinciaId = InsertProvince(provincia, conn);
                        provinceDict[provincia] = provinciaId;
                        Console.WriteLine($"Inserted province: {provincia} with ID: {provinciaId}");
                    }


                    if (!localityDict.ContainsKey(municipio))
                    {
                        int localidadId = InsertLocality(municipio, provinceDict[provincia], conn);
                        localityDict[municipio] = localidadId;
                        Console.WriteLine($"Inserted locality: {municipio} with ID: {localidadId}");
                    }

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
}