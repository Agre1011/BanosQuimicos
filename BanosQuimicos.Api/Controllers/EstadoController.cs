using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanosQuimicos.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/estado")]
public class EstadoController : ControllerBase
{
    [HttpGet]
    public IActionResult Obtener()
    {
        return Ok(new { mensaje = "La API de Baños Químicos está funcionando." });
    }
}
