using Microsoft.AspNetCore.Mvc;
using Serilog;
using Backend.Api.GAL.Logic;
using System.IO;

namespace Backend.Api.GAL.Controllers
{
    [ApiController]
    [Route("api/gal")]
    public class DataController : ControllerBase
    {
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
                Log.Error(ex, "API GAL: Error durante la transformación");
                return Problem(ex.Message);
            }
        }
    }
}