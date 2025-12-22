using Microsoft.AspNetCore.Mvc;
using Serilog;
using Backend.Api.CAT.Logic;
using System.IO;

namespace Backend.Api.CAT.Controllers
{
    [ApiController]
    [Route("api/cat")]
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
                Log.Error(ex, "API CAT: Error durante la transformación");
                return Problem(ex.Message);
            }
        }
    }
}