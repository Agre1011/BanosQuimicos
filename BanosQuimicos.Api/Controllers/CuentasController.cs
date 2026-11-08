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
public partial class CuentasController(BanosQuimicosDbContext db) : ControllerBase
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
                x.EstadoCuenta == "Pendiente" && (x.Rol.NombreRol == "Cliente" || x.Rol.NombreRol == "Empleado") && x.Cliente != null, x.MotivoSuspension, x.FechaBaja))
            .ToListAsync(ct);
        return Ok(new PaginaCuentasRespuesta(elementos, consulta.Pagina, consulta.TamanoPagina, total));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CuentaGestionRespuesta>> Detalle(int id, CancellationToken ct)
    {
        var cuenta = await db.Usuarios.AsNoTracking().Where(x => x.IdUsuario == id)
            .Select(x => new CuentaGestionRespuesta(x.IdUsuario, x.Nombre, x.Apellido, x.Email,
                x.Telefono, x.Rol.NombreRol, x.EstadoCuenta, x.FechaRegistro,
                x.EstadoCuenta == "Pendiente" && (x.Rol.NombreRol == "Cliente" || x.Rol.NombreRol == "Empleado") && x.Cliente != null, x.MotivoSuspension, x.FechaBaja))
            .SingleOrDefaultAsync(ct);
        return cuenta is null ? NotFound() : Ok(cuenta);
    }

}
