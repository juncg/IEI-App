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

        public void Run(List<UnifiedData> data)
        {
            string dbPath = DatabaseHelper.GetDatabasePath("databases/iei.db");
            string connectionString = DatabaseHelper.GetConnectionString(dbPath);

            // Asegurar que el directorio existe
            string? dbDir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }

            // Eliminar base de datos existente si existe para empezar de cero
            if (File.Exists(dbPath))
            {
                try
                {
                    File.Delete(dbPath);
                    Log.Information("Base de datos existente eliminada: {DbPath}", dbPath);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "No se pudo eliminar la base de datos existente: {DbPath}", dbPath);
                }
            }

            Log.Information("Empezando inserción en la base de datos. Registros totales: {RecordCount}", data.Count);
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            DatabaseInitializer.Initialize(conn);
            Log.Information("Base de datos inicializada.");

            var provinceCache = new Dictionary<string, int>();
            var localityCache = new Dictionary<string, int>();

            using var transaction = conn.BeginTransaction();
            try
            {
                foreach (var item in data)
                {
                    Log.Debug("Procesando registro: {@Record}", item);

                    string provName = string.IsNullOrWhiteSpace(item.ProvinceName) ? "Desconocida" : item.ProvinceName.Trim();
                    if (!provinceCache.TryGetValue(provName, out int provinceId))
                    {
                        provinceId = _locationRepository.GetOrInsertProvince(conn, provName, transaction);
                        provinceCache[provName] = provinceId;
                        Log.Information("Provincia insertada: {ProvinceName} con ID {ProvinceId}", provName, provinceId);
                    }

                    string locName = string.IsNullOrWhiteSpace(item.LocalityName) ? "Desconocida" : item.LocalityName.Trim();
                    string locKey = $"{provinceId}-{locName}";
                    if (!localityCache.TryGetValue(locKey, out int localityId))
                    {
                        localityId = _locationRepository.GetOrInsertLocality(conn, locName, provinceId, transaction);
                        localityCache[locKey] = localityId;
                        Log.Information("Localidad insertada: {LocalityName} con ID {LocalityId}", locName, localityId);
                    }

                    _stationRepository.InsertStation(conn, item.Station, localityId, transaction);
                    Log.Information("Estación insertada: {StationName}", item.Station.name);
                }

                transaction.Commit();
                Log.Information("Población de la base de datos completada con éxito.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "Error al insertar datos.");
                throw;
            }
        }
    }
}
