using Backend.Models;
using Backend.Repositories;
using Microsoft.Data.Sqlite;
using Serilog;

namespace Backend.Services
{
    public class DataInserter
    {
        private readonly LocationRepository _locationRepository;
        private readonly StationRepository _stationRepository;

        public DataInserter()
        {
            _locationRepository = new LocationRepository();
            _stationRepository = new StationRepository();
        }

        public LoadResultDto Run(List<UnifiedData> data)
        {
            var result = new LoadResultDto();
            string dbPath = DatabaseHelper.GetDatabasePath("databases/iei.db");
            string connectionString = DatabaseHelper.GetConnectionString(dbPath);

            // Asegurar que el directorio existe
            string? dbDir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }

            Log.Information("Paso Inserter: Iniciando inserción en la base de datos. Registros totales: {RecordCount}", data.Count);
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            DatabaseInitializer.Initialize(conn);
            Log.Information("Paso Inserter: Base de datos inicializada (tablas borradas y recreadas).");

            var provinceCache = new Dictionary<string, int>();
            var localityCache = new Dictionary<string, int>();

            using var transaction = conn.BeginTransaction();
            try
            {
                foreach (var item in data)
                {
                    Log.Debug("Procesando registro: {@Record}", item);

                    string provName = string.IsNullOrWhiteSpace(item.ProvinceName) ? "Desconocida" : item.ProvinceName.Trim();

                    int? provinceId = null;
                    if (provName != "Desconocida" && !string.IsNullOrWhiteSpace(provName))
                    {
                        if (!provinceCache.TryGetValue(provName, out int cachedProvinceId))
                        {
                            cachedProvinceId = _locationRepository.GetOrInsertProvince(conn, provName, transaction);
                            provinceCache[provName] = cachedProvinceId;
                            Log.Information("Provincia insertada: {ProvinceName} con ID {ProvinceId}", provName, cachedProvinceId);
                        }
                        provinceId = cachedProvinceId;
                    }

                    int? localityId = null;
                    string locName = item.LocalityName?.Trim() ?? "";

                    if (provinceId.HasValue && !string.IsNullOrWhiteSpace(locName))
                    {
                        string locKey = $"{provinceId.Value}-{locName}";
                        if (!localityCache.TryGetValue(locKey, out int cachedLocalityId))
                        {
                            cachedLocalityId = _locationRepository.GetOrInsertLocality(conn, locName, provinceId.Value, transaction);
                            localityCache[locKey] = cachedLocalityId;
                            Log.Information("Localidad insertada: {LocalidadName} con ID {LocalidadId}", locName, cachedLocalityId);
                        }
                        localityId = cachedLocalityId;
                    }

                    try
                    {
                        var action = _stationRepository.InsertStation(conn, item.Station, localityId, transaction);
                        if (action == "inserted")
                        {
                            result.RecordsLoadedCorrectly++;
                            Log.Information("Estación insertada: {StationName}", item.Station.name);
                        }
                        else if (action == "duplicated")
                        {
                            result.RecordsDiscarded++;
                            result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "DB", Name = item.Station.name, Locality = item.LocalityName, ErrorReason = "Estación duplicada (mismo nombre y tipo ya existe)" });
                            Log.Warning("Estación duplicada descartada: {StationName}", item.Station.name);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.RecordsDiscarded++;
                        result.DiscardedRecords.Add(new DiscardedRecord { DataSource = "DB", Name = item.Station.name, Locality = item.LocalityName, ErrorReason = ex.Message });
                        Log.Error(ex, "Error insertando estación: {StationName}", item.Station.name);
                    }
                }

                transaction.Commit();
                Log.Information("Población de la base de datos completada. Insertados: {Inserted}, Descartados: {Discarded}", result.RecordsLoadedCorrectly, result.RecordsDiscarded);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "Paso Inserter: Error al insertar datos.");
                throw;
            }
            return result;
        }
    }
}
