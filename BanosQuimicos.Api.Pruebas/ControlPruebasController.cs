using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BanosQuimicos.Api.Pruebas;

[ApiController]
[Route("_pruebas")]
public class ControlPruebasController : ControllerBase
{
    [HttpGet("operacion"), Authorize] public IActionResult Operacion() => NoContent();
    [HttpGet("sin-atributo")] public IActionResult SinAtributo() => NoContent();
    [HttpGet("empleado"), Authorize(Roles = "Empleado")] public IActionResult Empleado() => NoContent();
}
