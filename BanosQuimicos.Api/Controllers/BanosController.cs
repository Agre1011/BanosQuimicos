using System.Data;
using System.Security.Claims;
using BanosQuimicos.Api.Contratos.Banos;
using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Modelos;
using BanosQuimicos.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Controllers;

[ApiController]
[Route("api/banos")]
[Authorize(Policy = Politicas.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class BanosController(BanosQuimicosDbContext db, TimeProvider reloj) : ControllerBase
{
    [HttpGet("empleados-asignables")]
    public async Task<ActionResult<List<EmpleadoAsignableRespuesta>>> Empleados(CancellationToken ct) =>
        Ok(await db.Usuarios.AsNoTracking().Where(x => x.EstadoCuenta == "Activa" && x.Rol.NombreRol == "Empleado")
            .OrderBy(x => x.Apellido).ThenBy(x => x.Nombre).ThenBy(x => x.IdUsuario)
            .Select(x => new EmpleadoAsignableRespuesta(x.IdUsuario, x.Nombre + " " + x.Apellido)).ToListAsync(ct));

    private IQueryable<EstadoBanoRespuesta> Estados(int? id = null) =>
        from b in db.BanosQuimicos.AsNoTracking()
        where id == null || b.IdBano == id
        let servicio = db.Servicios.Where(s => s.IdBano == b.IdBano && (s.EstadoServicio == "Entregado" || s.EstadoServicio == "Pendiente de entrega")).FirstOrDefault()
        let ubicacion = db.UbicacionesServicio.Where(u => servicio != null && u.IdServicio == servicio.IdServicio && u.EsActual).FirstOrDefault()
        let mantenimiento = db.TareasOperativas.Any(t => t.IdBano == b.IdBano && t.BloqueaAsignacion && (t.IdEstadoOperacion == 6 || t.IdEstadoOperacion == 7))
        orderby b.CodigoInterno, b.IdBano
        select new EstadoBanoRespuesta(b.IdBano, b.CodigoInterno, b.TipoBano.NombreTipo, b.EstadoBano.NombreEstado,
            mantenimiento, b.EstadoBano.NombreEstado == "Apto" && servicio == null && !mantenimiento,
            b.IdResponsableLimpieza, b.ResponsableLimpieza == null ? null : b.ResponsableLimpieza.Nombre + " " + b.ResponsableLimpieza.Apellido,
            b.ResponsableLimpieza != null && b.ResponsableLimpieza.EstadoCuenta == "Activa" && b.ResponsableLimpieza.Rol.NombreRol == "Empleado",
            servicio == null ? null : servicio.IdServicio, servicio == null ? null : servicio.EstadoServicio,
            servicio == null ? null : servicio.TipoUso, servicio == null ? null : servicio.Cliente.Usuario.Nombre + " " + servicio.Cliente.Usuario.Apellido,
            ubicacion == null ? null : ubicacion.IdUbicacion, ubicacion == null ? null : ubicacion.Direccion,
            servicio == null ? null : servicio.CantidadLimpiezasSemana);

    [HttpGet("estado")]
    public async Task<ActionResult<List<EstadoBanoRespuesta>>> Listar(CancellationToken ct) => Ok(await Estados().ToListAsync(ct));

    [HttpPut("{id:int}/responsable-limpieza")]
    public async Task<ActionResult<EstadoBanoRespuesta>> Responsable(int id, AsignarResponsableSolicitud datos, CancellationToken ct)
    {
        await using var tx = await Bloquear(ct);
        if (!await AdminActivo(ct)) return Forbid();
        var bano = await db.BanosQuimicos.SingleOrDefaultAsync(x => x.IdBano == id, ct);
        if (bano is null) return NotFound();
        if (!await EmpleadoActivo(datos.IdEmpleado, ct)) return Conflict(new { mensaje = "Elegir un empleado activo." });
        // Repetición del mismo destino es inocua; otro cambio concurrente no se pisa.
        if (bano.IdResponsableLimpieza != datos.IdEmpleado && bano.IdResponsableLimpieza != datos.IdResponsableAnterior)
            return Conflict(new { mensaje = "El responsable cambió. Volvé a consultar la tarjeta." });
        bano.IdResponsableLimpieza = datos.IdEmpleado;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Ok(await Estados(id).SingleAsync(ct));
    }

    [HttpPost("{id:int}/limpiezas")]
    public async Task<IActionResult> Programar(int id, ProgramarLimpiezaSolicitud datos, CancellationToken ct)
    {
        if (datos.ClaveProgramacion == Guid.Empty || datos.Fecha == default)
            return BadRequest(new { mensaje = "Indicar fecha y clave de programación." });
        // La pantalla elige un día, no una hora de realización ficticia.
        var fecha = new DateTimeOffset(datos.Fecha.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-3));
        await using var tx = await Bloquear(ct);
        if (!await AdminActivo(ct)) return Forbid();
        var previa = await db.TareasOperativas.SingleOrDefaultAsync(x => x.ClaveProgramacion == datos.ClaveProgramacion, ct);
        if (previa is not null)
        {
            if (previa.IdBano != id || previa.IdEmpleado != datos.IdEmpleado || previa.IdServicio != datos.IdServicio || previa.IdUbicacion != datos.IdUbicacion || previa.FechaProgramada != fecha)
                return Conflict(new { mensaje = "La clave ya corresponde a otra programación." });
            await tx.CommitAsync(ct);
            return Ok(new { previa.IdTarea });
        }
        if (!await db.BanosQuimicos.AnyAsync(x => x.IdBano == id, ct)) return NotFound();
        if (!await EmpleadoActivo(datos.IdEmpleado, ct)) return Conflict(new { mensaje = "Elegir un empleado activo." });
        var servicio = await db.Servicios.SingleOrDefaultAsync(x => x.IdServicio == datos.IdServicio && x.IdBano == id && x.EstadoServicio == "Entregado", ct);
        if (servicio is null) return Conflict(new { mensaje = "La limpieza requiere el servicio entregado que muestra la tarjeta. Volvé a consultar." });
        if (!await db.UbicacionesServicio.AnyAsync(x => x.IdUbicacion == datos.IdUbicacion && x.IdServicio == servicio.IdServicio && x.EsActual, ct))
            return Conflict(new { mensaje = "La ubicación cambió o no está disponible. Volvé a consultar." });
        // También detectar el mismo envío recreado tras navegar o reiniciar la app.
        var duplicada = await db.TareasOperativas.Where(x => x.IdBano == id && x.IdServicio == datos.IdServicio
            && x.IdUbicacion == datos.IdUbicacion && x.IdEmpleado == datos.IdEmpleado && x.FechaProgramada == fecha
            && x.IdTipoOperacion == 9 && x.IdEstadoOperacion != 9).Select(x => (int?)x.IdTarea).FirstOrDefaultAsync(ct);
        if (duplicada is not null) return Conflict(new { mensaje = "Ya existe esa limpieza para el mismo empleado, ubicación y fecha.", idTarea = duplicada });
        // Un responsable habitual no sustituye al empleado de cada trabajo ni se cambia por esta operación.
        // No apto/bloqueo impiden nuevas asignaciones de servicio, no esta limpieza de uno entregado.
        var tarea = new TareaOperativa { IdBano = id, IdServicio = servicio.IdServicio, IdUbicacion = datos.IdUbicacion,
            IdEmpleado = datos.IdEmpleado, IdTipoOperacion = 9, IdEstadoOperacion = 6,
            FechaProgramada = fecha, FechaRegistro = reloj.GetUtcNow(), ClaveProgramacion = datos.ClaveProgramacion };
        db.TareasOperativas.Add(tarea); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return StatusCode(StatusCodes.Status201Created, new { tarea.IdTarea });
    }

    private Task<bool> EmpleadoActivo(int id, CancellationToken ct) => db.Usuarios.AnyAsync(x => x.IdUsuario == id && x.EstadoCuenta == "Activa" && x.Rol.NombreRol == "Empleado", ct);
    private Task<bool> AdminActivo(CancellationToken ct)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return db.Usuarios.AnyAsync(x => x.IdUsuario == id && x.EstadoCuenta == "Activa" && x.Rol.NombreRol == "Administrador", ct);
    }
    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> Bloquear(CancellationToken ct)
    {
        var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            // Mismo orden que el cumplimiento de tareas: no programar sobre un servicio a medio cerrar.
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @resultado int;
                EXEC @resultado=sys.sp_getapplock @Resource=N'BanosQuimicos.CumplimientoTareas',
                    @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
                IF @resultado<0 THROW 51000, 'No se pudo bloquear la programación.', 1;
                """, ct);
            return tx;
        }
        catch { await tx.DisposeAsync(); throw; }
    }
}
