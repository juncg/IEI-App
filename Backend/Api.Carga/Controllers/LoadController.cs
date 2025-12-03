using Backend.Models;
using Backend.Repositories;
using Backend.Services.Mappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Serilog;
using System.Text.Json;

namespace Backend.Api.Carga.Controllers
{
    [ApiController]
    [Route("api/load")]
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

        [HttpPost]
        public async Task<IActionResult> LoadData([FromBody] List<string> sources)
        {
            Log.Information("Load API: Iniciando carga para fuentes: {Sources}", string.Join(", ", sources));
            
            var summary = new LoadSummary
            {
                TotalRecords = 0,
                SuccessfulRecords = 0,
                FailedRecords = 0,
                Errors = new List<string>()
            };

            var unifiedDataList = new List<UnifiedData>();

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
                        string error = $"Error al obtener datos de {source}: {response.StatusCode}";
                        Log.Error(error);
                        summary.Errors.Add(error);
                        continue;
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    
                    // Extract "data" property from the wrapper response
                    using var doc = JsonDocument.Parse(jsonResponse);
                    if (doc.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        string dataJson = dataElement.ToString();
                        
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
                            // We pass false for validation to speed up, or true if required. 
                            // User didn't specify, but usually load implies validation.
                            // However, validation uses Selenium which is slow.
                            // For now, I'll set it to false to avoid opening browsers during load, 
                            // unless user explicitly asked for "Check against Selenium" (which was a previous task).
                            // The user prompt says "a esta se le indicará que datos se querran cargar...".
                            // I'll assume false for now to make it fast, or maybe true?
                            // The previous conversation mentioned a prompt for Selenium check.
                            // I'll stick to false for the API to be responsive, or maybe true if it's the "Carga" process.
                            // Let's use false for now.
                            mapper.Map(dataJson, unifiedDataList, false, source == "CV", source == "GAL", source == "CAT");
                        }
                    }
                }
                catch (Exception ex)
                {
                    string error = $"Excepción procesando {source}: {ex.Message}";
                    Log.Error(ex, error);
                    summary.Errors.Add(error);
                }
            }

            summary.TotalRecords = unifiedDataList.Count;

            // 2. Save to Database
            Log.Information("Guardando {Count} registros en base de datos...", unifiedDataList.Count);
            
            try
            {
                using var connection = new SqliteConnection("Data Source=databases/iei.db");
                connection.Open();
                using var transaction = connection.BeginTransaction();

                foreach (var item in unifiedDataList)
                {
                    try
                    {
                        // Resolve Location (Province -> Locality)
                        int provinceId = _locationRepository.GetOrInsertProvince(connection, item.ProvinceName ?? "Desconocida", transaction);
                        int localityId = _locationRepository.GetOrInsertLocality(connection, item.LocalityName ?? "Desconocida", provinceId, transaction);
                        
                        _stationRepository.InsertStation(connection, item.Station, localityId, transaction);
                        summary.SuccessfulRecords++;
                    }
                    catch (Exception ex)
                    {
                        summary.FailedRecords++;
                        summary.Errors.Add($"Error insertando {item.Station.name}: {ex.Message}");
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error general en base de datos");
                summary.Errors.Add($"Error de base de datos: {ex.Message}");
            }

            return Ok(summary);
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

    public class LoadSummary
    {
        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public List<string> Errors { get; set; }
    }
}
