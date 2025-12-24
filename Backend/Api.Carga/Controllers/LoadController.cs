using Backend.Api.Carga.Logic;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Backend.Api.Carga.Controllers
{
    [ApiController]
    [Route("api")]
    [ApiExplorerSettings(GroupName = "Load")]
    public class LoadController : ControllerBase
    {
        private readonly LoadService _loadService;

        public LoadController(LoadService loadService)
        {
            _loadService = loadService;
        }

        /// <summary>
        /// Carga datos de estaciones desde las APIs de transformación (CAT, CV, GAL) y los inserta en la base de datos.
        /// </summary>
        /// <param name="sources">Lista de fuentes de datos a cargar (ej: ["CAT", "CV", "GAL"]).</param>
        /// <returns>Resultado del proceso de carga incluyendo estadísticas de registros procesados, reparados y descartados.</returns>
        [HttpPost("load")]
        public async Task<IActionResult> LoadData([FromBody] List<string> sources)
        {
            try
            {
                var result = await _loadService.LoadDataAsync(sources);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error en carga de datos");
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Limpia completamente la base de datos, eliminando todas las tablas y recreándolas vacías.
        /// </summary>
        /// <returns>Mensaje de confirmación de que la base de datos ha sido limpiada.</returns>
        [HttpPost("clear")]
        public IActionResult ClearDatabase()
        {
            try
            {
                var message = _loadService.ClearDatabase();
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al limpiar la base de datos");
                return StatusCode(500, new { error = "Error al limpiar la base de datos", details = ex.Message });
            }
        }
    }
}
