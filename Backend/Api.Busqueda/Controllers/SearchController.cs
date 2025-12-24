using Backend.Api.Busqueda.Logic;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Backend.Api.Busqueda.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly SearchService _searchService;

        public SearchController(SearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet]
        public IActionResult Search([FromQuery] string? name, [FromQuery] int? type, [FromQuery] string? locality, [FromQuery] string? postalCode, [FromQuery] string? province)
        {
            try
            {
                var results = _searchService.SearchStations(name, type, locality, postalCode, province);
                return Ok(results);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error en búsqueda");
                return Problem(ex.Message);
            }
        }

        [HttpGet("stations")]
        public IActionResult GetStations()
        {
            try
            {
                var results = _searchService.GetStationsWithCoordinates();
                return Ok(results);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obteniendo estaciones");
                return Problem(ex.Message);
            }
        }
    }
}
