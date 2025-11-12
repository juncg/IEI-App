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
using Backend.Models;

namespace Backend.Inserter;

public class Inserter
{
    static int InsertProvince(string name, SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Provincia (nombre) VALUES (@name); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@name", name);
        long id = (long)cmd.ExecuteScalar();
        return (int)id;
    }

    static int InsertLocality(string name, int provinceCode, SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Localidad (nombre, codigo_provincia) VALUES (@name, @province_code); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@province_code", provinceCode);
        long id = (long)cmd.ExecuteScalar();
        return (int)id;
    }

    static int InsertStation(Station station, SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Estacion
                (codigo, nombre, tipo, direccion, codigo_postal, longitud, latitud, descripcion, horario, contacto, url, codigo_localidad)
            VALUES
                (@codigo, @nombre, @tipo, @direccion, @codigo_postal, @longitud, @latitud, @descripcion, @horario, @contacto, @url, @codigo_localidad);
            SELECT last_insert_rowid();";

        cmd.Parameters.AddWithValue("@codigo", station.code);
        cmd.Parameters.AddWithValue("@nombre", station.name ?? string.Empty);
        cmd.Parameters.AddWithValue("@tipo", station.type.ToString());
        cmd.Parameters.AddWithValue("@direccion", station.address ?? string.Empty);
        cmd.Parameters.AddWithValue("@codigo_postal", station.postal_code ?? string.Empty);

        if (station.longitude.HasValue)
            cmd.Parameters.AddWithValue("@longitud", station.longitude.Value);
        else
            cmd.Parameters.AddWithValue("@longitud", DBNull.Value);

        if (station.latitude.HasValue)
            cmd.Parameters.AddWithValue("@latitud", station.latitude.Value);
        else
            cmd.Parameters.AddWithValue("@latitud", DBNull.Value);

        cmd.Parameters.AddWithValue("@descripcion", station.description ?? string.Empty);
        cmd.Parameters.AddWithValue("@horario", station.schedule ?? string.Empty);
        cmd.Parameters.AddWithValue("@contacto", station.contact ?? string.Empty);
        cmd.Parameters.AddWithValue("@url", station.url ?? string.Empty);

        if (station.locality_code != 0)
            cmd.Parameters.AddWithValue("@codigo_localidad", station.locality_code);
        else
            cmd.Parameters.AddWithValue("@codigo_localidad", DBNull.Value);

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