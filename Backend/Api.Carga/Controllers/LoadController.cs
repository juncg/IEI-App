using Backend.Models;
using Backend.Repositories;
using Backend.Services;
using Backend.Services.Mappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Serilog;
using System.Text.Json;

namespace Backend.Api.Carga.Controllers
{
    [ApiController]
    [Route("api")]
    public class LoadController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly StationRepository _stationRepository;
        private readonly LocationRepository _locationRepository;
        private readonly IEnumerable<IMapper> _mappers;

        public LoadController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _stationRepository = new StationRepository();
            _locationRepository = new LocationRepository();

            // Initialize mappers manually or via DI if registered
            _mappers = new List<IMapper>
            {
                new CATMapper(),
                new CVMapper(),
                new GALMapper()
            };
        }

        [HttpPost("load")]
        public async Task<IActionResult> LoadData([FromBody] List<string> sources)
        {
            Log.Information("Load API: Iniciando carga para fuentes: {Sources}", string.Join(", ", sources));

            var loadResult = new LoadResultDto();
            var unifiedDataList = new List<UnifiedData>();
            var seenStations = new HashSet<string>();
            var repairedDict = new Dictionary<string, RepairedRecord>();

            using var client = _httpClientFactory.CreateClient();

            // 1. Fetch and Map Data
            foreach (var source in sources)
            {
                try
                {
                    string url = GetUrlForSource(source);
                    if (string.IsNullOrEmpty(url))
                    {
                        Log.Warning("Fuente desconocida: {Source}", source);
                        continue;
                    }

                    Log.Information("Obteniendo datos de {Source} en {Url}...", source, url);
                    var response = await client.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        loadResult.DiscardedRecords.Add(new DiscardedRecord { DataSource = source, Name = "N/A", Locality = "N/A", ErrorReason = $"Error al obtener datos: {response.StatusCode}" });
                        loadResult.RecordsDiscarded++;
                        continue;
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    // Extract "data" property from the wrapper response
                    using var doc = JsonDocument.Parse(jsonResponse);
                    if (doc.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        string dataJson;
                        if (source.ToUpper() == "GAL")
                        {
                            // For GAL, data is {"establishments": [...]}, so extract the array
                            if (dataElement.TryGetProperty("establishments", out var establishments))
                            {
                                dataJson = establishments.ToString();
                            }
                            else
                            {
                                Log.Warning("No se encontraron 'establishments' en la respuesta de {Source}", source);
                                continue;
                            }
                        }
                        else
                        {
                            // For CAT and CV, data is the direct content
                            dataJson = dataElement.ToString();
                        }

                        // Select Mapper
                        IMapper? mapper = source.ToUpper() switch
                        {
                            "CAT" => _mappers.OfType<CATMapper>().FirstOrDefault(),
                            "CV" => _mappers.OfType<CVMapper>().FirstOrDefault(),
                            "GAL" => _mappers.OfType<GALMapper>().FirstOrDefault(),
                            _ => null
                        };

                        if (mapper != null)
                        {
                            Log.Information("Mapeando datos de {Source}...", source);
                            var mapResult = mapper.Map(dataJson, false, source == "CV", source == "GAL", source == "CAT");

                            foreach (var unified in mapResult.UnifiedData)
                            {
                                string key = $"{unified.Station.name}_{unified.Station.type}";
                                if (!seenStations.Contains(key))
                                {
                                    unifiedDataList.Add(unified);
                                    seenStations.Add(key);
                                }
                                else
                                {
                                    loadResult.DiscardedRecords.Add(new DiscardedRecord { DataSource = source, Name = unified.Station.name, Locality = unified.LocalityName, ErrorReason = "Estación duplicada en datos fuente" });
                                    loadResult.RecordsDiscarded++;
                                }
                            }

                            foreach (var repaired in mapResult.RepairedRecords)
                            {
                                string key = $"{repaired.DataSource}_{repaired.Name}_{repaired.Locality}";
                                if (repairedDict.TryGetValue(key, out var existing))
                                {
                                    foreach (var op in repaired.Operations)
                                    {
                                        if (!existing.Operations.Any(e => e.ErrorReason == op.ErrorReason && e.OperationPerformed == op.OperationPerformed))
                                        {
                                            existing.Operations.Add(op);
                                        }
                                    }
                                }
                                else
                                {
                                    repairedDict[key] = repaired;
                                }
                            }

                            loadResult.DiscardedRecords.AddRange(mapResult.DiscardedRecords);
                            loadResult.RecordsDiscarded += mapResult.DiscardedRecords.Count;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Excepción procesando {Source}: {Message}", source, ex.Message);
                    loadResult.DiscardedRecords.Add(new DiscardedRecord { DataSource = source, Name = "N/A", Locality = "N/A", ErrorReason = $"Excepción procesando: {ex.Message}" });
                    loadResult.RecordsDiscarded++;
                }
            }

            Log.Information("Guardando {Count} registros en base de datos...", unifiedDataList.Count);

            var dataInserter = new DataInserter();
            var insertResult = dataInserter.Run(unifiedDataList);

            loadResult.RepairedRecords.AddRange(repairedDict.Values);
            loadResult.RecordsLoadedCorrectly = insertResult.RecordsLoadedCorrectly;
            loadResult.RecordsRepaired = loadResult.RepairedRecords.Count;
            loadResult.DiscardedRecords.AddRange(insertResult.DiscardedRecords);
            loadResult.RecordsDiscarded += insertResult.RecordsDiscarded;

            return Ok(loadResult);
        }

        [HttpPost("clear")]
        public IActionResult ClearDatabase()
        {
            Log.Information("Load API: Iniciando limpieza de la base de datos");

            try
            {
                string dbPath = DatabaseHelper.GetDatabasePath("databases/iei.db");
                string connectionString = DatabaseHelper.GetConnectionString(dbPath);

                // Asegurar que el directorio existe
                string? dbDir = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
                {
                    Directory.CreateDirectory(dbDir);
                }

                using var conn = new SqliteConnection(connectionString);
                conn.Open();

                DatabaseInitializer.Initialize(conn);
                Log.Information("Load API: Base de datos limpiada exitosamente (tablas borradas y recreadas)");

                return Ok(new { message = "Base de datos limpiada exitosamente" });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Load API: Error al limpiar la base de datos");
                return StatusCode(500, new { error = "Error al limpiar la base de datos", details = ex.Message });
            }
        }

        private string GetUrlForSource(string source)
        {
            return source.ToUpper() switch
            {
                "CAT" => "http://localhost:5001/api/cat/transform",
                "CV" => "http://localhost:5002/api/cv/transform",
                "GAL" => "http://localhost:5003/api/gal/transform",
                _ => ""
            };
        }
    }
}
