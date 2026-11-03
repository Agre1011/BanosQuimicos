using BanosQuimicos.Api.Contratos.Cuentas;
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
    [HttpGet]
    public async Task<ActionResult<PaginaCuentasRespuesta>> Listar([FromQuery] ConsultaCuentas consulta, CancellationToken ct)
    {
        var salto = (long)(consulta.Pagina - 1) * consulta.TamanoPagina;
        if (salto > int.MaxValue) return BadRequest(new { mensaje = "La página solicitada es demasiado grande." });
        var usuarios = db.Usuarios.AsNoTracking();
        var busqueda = consulta.Busqueda?.Trim();
        if (!string.IsNullOrEmpty(busqueda))
            usuarios = usuarios.Where(x => x.Nombre.Contains(busqueda) || x.Apellido.Contains(busqueda) || x.Email.Contains(busqueda));
        if (!string.IsNullOrEmpty(consulta.Estado))
            usuarios = usuarios.Where(x => x.EstadoCuenta == consulta.Estado);
        if (!string.IsNullOrEmpty(consulta.Rol))
            usuarios = usuarios.Where(x => x.Rol.NombreRol == consulta.Rol);
        var total = await usuarios.CountAsync(ct);
        // IdUsuario desempata nombres iguales: el orden no depende del plan de ejecución SQL.
        var elementos = await usuarios.OrderBy(x => x.Apellido).ThenBy(x => x.Nombre).ThenBy(x => x.IdUsuario)
            .Skip((int)salto).Take(consulta.TamanoPagina)
            .Select(x => new CuentaGestionRespuesta(x.IdUsuario, x.Nombre, x.Apellido, x.Email,
                x.Telefono, x.Rol.NombreRol, x.EstadoCuenta, x.FechaRegistro,
                x.EstadoCuenta == "Pendiente" && x.Rol.NombreRol == "Cliente" && x.Cliente != null))
            .ToListAsync(ct);
        return Ok(new PaginaCuentasRespuesta(elementos, consulta.Pagina, consulta.TamanoPagina, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CuentaGestionRespuesta>> Detalle(int id, CancellationToken ct)
    {
        var cuenta = await db.Usuarios.AsNoTracking().Where(x => x.IdUsuario == id)
            .Select(x => new CuentaGestionRespuesta(x.IdUsuario, x.Nombre, x.Apellido, x.Email,
                x.Telefono, x.Rol.NombreRol, x.EstadoCuenta, x.FechaRegistro,
                x.EstadoCuenta == "Pendiente" && x.Rol.NombreRol == "Cliente" && x.Cliente != null))
            .SingleOrDefaultAsync(ct);
        return cuenta is null ? NotFound() : Ok(cuenta);
    }

    [HttpPut("{id:int}/rol")]
    public async Task<IActionResult> CambiarRol(int id, CambioRolSolicitud datos, CancellationToken ct)
    {
        if (datos.RolAnterior == datos.NuevoRol)
            return BadRequest(new { mensaje = "Elegir un rol distinto del actual." });
        var destino = await db.Roles.Where(x => x.NombreRol == datos.NuevoRol).Select(x => x.IdRol).SingleAsync(ct);
        // Solo se cambia la FK de rol: estado, contraseña, perfil e historial permanecen intactos.
        // El rol anterior evita sobrescribir silenciosamente el cambio de otra sesión.
        var actualizadas = await db.Usuarios.Where(x => x.IdUsuario == id && x.Rol.NombreRol == datos.RolAnterior)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IdRol, destino), ct);
        if (actualizadas == 1) return NoContent();
        if (!await db.Usuarios.AnyAsync(x => x.IdUsuario == id, ct)) return NotFound();
        return Conflict(new { mensaje = "El rol cambió o la cuenta no es Cliente/Empleado. Consultá su estado actual." });
    }

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
