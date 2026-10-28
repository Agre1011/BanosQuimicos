using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Controllers;

[ApiController]
[Route("api/cuentas")]
[Authorize(Policy = Politicas.Administrador)]
public class CuentasController(BanosQuimicosDbContext db) : ControllerBase
{
    [HttpPost("{id:int}/activar")]
    public async Task<IActionResult> Activar(int id, CancellationToken ct)
    {
        // Una actualización condicional evita activar una cuenta que dejó de estar pendiente.
        var actualizadas = await db.Usuarios.Where(x => x.IdUsuario == id
                && x.EstadoCuenta == "Pendiente" && x.Rol.NombreRol == "Cliente" && x.Cliente != null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.EstadoCuenta, "Activa"), ct);
        if (actualizadas == 1) return NoContent();
        if (!await db.Usuarios.AnyAsync(x => x.IdUsuario == id, ct)) return NotFound();
        return Conflict(new { mensaje = "Solo se pueden activar clientes pendientes con perfil de cliente." });
    }
}
