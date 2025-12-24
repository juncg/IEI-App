using Microsoft.AspNetCore.Mvc;
using Serilog;
using Backend.Api.CV.Logic;
using System.IO;

namespace Backend.Api.CV.Controllers
{
    [ApiController]
    [Route("api/cv")]
    [ApiExplorerSettings(GroupName = "CV")]
    public class DataController : ControllerBase
    {
        /// <summary>
        /// Transforma los datos de estaciones ITV de Comunidad Valenciana desde formato JSON a JSON estructurado.
        /// </summary>
        /// <returns>Datos transformados de estaciones ITV de Comunidad Valenciana incluyendo metadatos del proceso.</returns>
        [HttpGet("transform")]
        public IActionResult Transform()
        {
            try
            {
                var result = Transformer.Transform();
                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "API CV: Error durante la transformación");
                return Problem(ex.Message);
            }
        }
    }
}