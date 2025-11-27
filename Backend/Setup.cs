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

}
