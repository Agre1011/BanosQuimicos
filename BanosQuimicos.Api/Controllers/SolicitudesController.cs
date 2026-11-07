using BanosQuimicos.Api.Contratos.Solicitudes;
using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using BanosQuimicos.Api.Modelos;

namespace BanosQuimicos.Api.Controllers;

[ApiController]
[Route("api/solicitudes")]
[Authorize(Policy = Politicas.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class SolicitudesController(BanosQuimicosDbContext db, TimeProvider reloj) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SolicitudResumenRespuesta>>> Listar(CancellationToken ct) =>
        Ok(await db.SolicitudesCliente.AsNoTracking().OrderByDescending(x => x.FechaSolicitud).ThenByDescending(x => x.IdSolicitud)
            .Select(x => new SolicitudResumenRespuesta(x.IdSolicitud, x.Cliente.Usuario.Nombre + " " + x.Cliente.Usuario.Apellido,
                x.TipoOperacion.NombreOperacion, x.EstadoOperacion.NombreEstado, x.FechaSolicitud)).ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SolicitudDetalleRespuesta>> Detalle(int id, CancellationToken ct)
    {
        var detalle = await db.SolicitudesCliente.AsNoTracking().Where(x => x.IdSolicitud == id)
            .Select(x => new SolicitudDetalleRespuesta(x.IdSolicitud, x.Cliente.Usuario.Nombre + " " + x.Cliente.Usuario.Apellido,
                x.IdTipoOperacion, x.TipoOperacion.NombreOperacion, x.IdEstadoOperacion, x.EstadoOperacion.NombreEstado,
                x.FechaSolicitud, x.Descripcion, x.IdServicioReferencia,
                x.ServicioReferencia == null ? null : x.ServicioReferencia.EstadoServicio,
                x.ServicioReferencia == null ? null : x.ServicioReferencia.Bano.CodigoInterno,
                db.UbicacionesServicio.Where(u => u.IdServicio == x.IdServicioReferencia && u.EsActual).Select(u => u.Direccion).FirstOrDefault(),
                x.NuevaDireccion, x.ModalidadCambioDireccion, x.FechaDeseada, x.FechaCambioEfectiva,
                x.FechaResolucion, x.ObservacionesAdministrador, x.CantidadBanos,
                x.TipoBanoSolicitado == null ? null : x.TipoBanoSolicitado.NombreTipo, x.TipoUsoSolicitado, x.DireccionSolicitada,
                db.TareasOperativas.Where(t => t.IdSolicitud == x.IdSolicitud).OrderBy(t => t.IdTarea)
                    .Select(t => new TrabajoSolicitudRespuesta(t.IdTarea, t.TipoOperacion.NombreOperacion, t.EstadoOperacion.NombreEstado,
                        t.Empleado == null ? null : t.Empleado.Nombre + " " + t.Empleado.Apellido, t.FechaProgramada)).ToList(),
                db.UbicacionesServicio.Where(u => u.IdServicio == x.IdServicioReferencia && u.EsActual).Select(u => (int?)u.IdUbicacion).FirstOrDefault()))
            .SingleOrDefaultAsync(ct);
        return detalle is null ? NotFound() : Ok(detalle);
    }

    [HttpPost("{id:int}/asignar")]
    public async Task<ActionResult<SolicitudDetalleRespuesta>> Asignar(int id, AsignarSolicitud datos, CancellationToken ct)
    {
        if (datos.ClaveProgramacion == Guid.Empty || datos.Fecha == default)
            return BadRequest(new { mensaje = "Indicar fecha y clave de la operación." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // El mismo orden de bloqueo que programación y cumplimiento evita cierres/asignaciones parciales.
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @r int;
            EXEC @r=sys.sp_getapplock @Resource=N'BanosQuimicos.CumplimientoTareas',
                @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
            IF @r<0 THROW 51000, 'No se pudo bloquear la asignación.', 1;
            """, ct);
        var admin = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!await db.Usuarios.AnyAsync(x => x.IdUsuario == admin && x.EstadoCuenta == "Activa" && x.Rol.NombreRol == "Administrador", ct)) return Forbid();
        var solicitud = await db.SolicitudesCliente.SingleOrDefaultAsync(x => x.IdSolicitud == id, ct);
        if (solicitud is null) return NotFound();
        // Catálogo estable: un pedido de servicio no se convierte en limpieza ni se aprueba aquí.
        var tipo = solicitud.IdTipoOperacion switch {
            3 => 3, 6 => 8,
            4 when solicitud.ModalidadCambioDireccion == "Solicitado" => 10,
            5 when datos.IdTipoTareaReclamo is 3 or 11 => datos.IdTipoTareaReclamo.Value,
            _ => 0
        };
        if (tipo == 0) return Conflict(new { mensaje = "Este tipo requiere otro tratamiento o elegir el trabajo del reclamo." });
        if (solicitud.IdTipoOperacion != 5 && datos.IdTipoTareaReclamo is not null || datos.BloqueaAsignacion && tipo != 11)
            return BadRequest(new { mensaje = "El tipo elegido o el bloqueo no corresponde a esta operación." });
        var fecha = new DateTimeOffset(datos.Fecha.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-3));
        var previa = await db.TareasOperativas.SingleOrDefaultAsync(x => x.ClaveProgramacion == datos.ClaveProgramacion, ct);
        if (previa is not null)
        {
            if (previa.IdSolicitud != id || previa.IdEmpleado != datos.IdEmpleado || previa.IdTipoOperacion != tipo || previa.BloqueaAsignacion != datos.BloqueaAsignacion
                || previa.IdServicio != datos.IdServicioEsperado || previa.IdUbicacion != datos.IdUbicacionEsperada || previa.FechaProgramada != fecha || previa.IdEstadoOperacion == 9)
                return Conflict(new { mensaje = "La clave corresponde a otra asignación o a una tarea cancelada." });
            await tx.CommitAsync(ct); return await Detalle(id, ct);
        }
        if (solicitud.IdEstadoOperacion != datos.IdEstadoAnterior || solicitud.IdEstadoOperacion is not (1 or 2))
            return Conflict(new { mensaje = "El estado de la solicitud cambió; consultá el detalle actual." });
        if (await db.TareasOperativas.AnyAsync(x => x.IdSolicitud == id, ct))
            return Conflict(new { mensaje = "La solicitud ya tiene trabajos. No se duplica ni reasigna su historial desde esta acción." });
        if (!await db.Usuarios.AnyAsync(x => x.IdUsuario == datos.IdEmpleado && x.EstadoCuenta == "Activa" && x.Rol.NombreRol == "Empleado", ct))
            return Conflict(new { mensaje = "Elegir un empleado activo." });
        if (solicitud.IdServicioReferencia != datos.IdServicioEsperado) return Conflict(new { mensaje = "El servicio de referencia cambió." });
        var servicio = await db.Servicios.SingleOrDefaultAsync(x => x.IdServicio == datos.IdServicioEsperado && x.IdCliente == solicitud.IdCliente && x.EstadoServicio == "Entregado", ct);
        if (servicio is null) return Conflict(new { mensaje = "La operación necesita un servicio entregado del mismo cliente." });
        if (!await db.UbicacionesServicio.AnyAsync(x => x.IdUbicacion == datos.IdUbicacionEsperada && x.IdServicio == servicio.IdServicio && x.EsActual, ct))
            return Conflict(new { mensaje = "La ubicación actual cambió o no está disponible." });
        var ahora = reloj.GetUtcNow();
        if (ahora < solicitud.FechaSolicitud) return Conflict(new { mensaje = "La solicitud tiene una fecha posterior a la fecha actual." });
        var tarea = new TareaOperativa { IdSolicitud = id, IdServicio = servicio.IdServicio, IdBano = servicio.IdBano,
            IdUbicacion = datos.IdUbicacionEsperada, IdEmpleado = datos.IdEmpleado, IdTipoOperacion = tipo,
            IdEstadoOperacion = 6, FechaRegistro = ahora, FechaProgramada = fecha,
            BloqueaAsignacion = datos.BloqueaAsignacion, ClaveProgramacion = datos.ClaveProgramacion };
        if (tipo == 10)
        {
            if (string.IsNullOrWhiteSpace(solicitud.NuevaDireccion)) return Conflict(new { mensaje = "El traslado no tiene dirección de destino." });
            // Solo preparar destino: su vigencia comenzará al finalizar el traslado, no al asignarlo.
            tarea.UbicacionDestino = new UbicacionServicio { IdServicio = servicio.IdServicio,
                Direccion = solicitud.NuevaDireccion, FechaRegistro = ahora, EsActual = false };
        }
        db.TareasOperativas.Add(tarea);
        solicitud.IdEstadoOperacion = 2; // Confirmación explícita «Aprobar y asignar», nunca Resuelta.
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return await Detalle(id, ct);
    }
}
