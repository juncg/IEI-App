using Backend.Models;
using Backend.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Serilog;

namespace Backend.Api.Busqueda.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly StationRepository _stationRepository;

        public SearchController()
        {
            _stationRepository = new StationRepository();
        }

        [HttpGet]
        public IActionResult Search([FromQuery] string? name, [FromQuery] string? type, [FromQuery] string? locality)
        {
            try
            {
                using var connection = new SqliteConnection("Data Source=databases/iei.db");
                connection.Open();

                var results = _stationRepository.SearchStations(connection, name, type, locality);
                return Ok(results);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error en búsqueda");
                return Problem(ex.Message);
            }
        }
    }
}
