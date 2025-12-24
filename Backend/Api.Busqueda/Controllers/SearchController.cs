using Backend.Api.Busqueda.Logic;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Backend.Api.Busqueda.Controllers
{
    [ApiController]
    [Route("api/search")]
    [ApiExplorerSettings(GroupName = "Search")]
    public class SearchController : ControllerBase
    {
        private readonly SearchService _searchService;

        public SearchController(SearchService searchService)
        {
            _searchService = searchService;
        }

        /// <summary>
        /// Busca estaciones de ITV basándose en criterios opcionales de filtrado.
        /// </summary>
        /// <param name="name">Nombre de la estación (búsqueda parcial, case-insensitive).</param>
        /// <param name="type">Tipo de estación (0: Fija, 1: Móvil, 2: Otros).</param>
        /// <param name="locality">Nombre de la localidad (búsqueda parcial, case-insensitive).</param>
        /// <param name="postalCode">Código postal (búsqueda parcial).</param>
        /// <param name="province">Nombre de la provincia (búsqueda parcial, case-insensitive).</param>
        /// <returns>Lista de estaciones que coinciden con los criterios de búsqueda.</returns>
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

        /// <summary>
        /// Obtiene todas las estaciones de ITV que tienen coordenadas (latitud y longitud) definidas.
        /// </summary>
        /// <returns>Lista de todas las estaciones con coordenadas para precargar en el mapa.</returns>
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
