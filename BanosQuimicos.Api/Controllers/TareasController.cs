using System.Security.Claims;
using BanosQuimicos.Api.Contratos.Tareas;
using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Modelos;
using BanosQuimicos.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BanosQuimicos.Api.Controllers;

[ApiController]
[Route("api/tareas")]
[Authorize(Policy = Politicas.Empleado)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class TareasController(BanosQuimicosDbContext db, TimeProvider reloj) : ControllerBase
{
    // Nunca aceptar IdEmpleado desde la app: se obtiene de la identidad validada por la API.
    private IQueryable<TareaOperativa> MisTareas()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return db.TareasOperativas.AsNoTracking().Where(x => x.IdEmpleado == id);
    }
    private IQueryable<TareaAsignadaRespuesta> Datos(IQueryable<TareaOperativa> tareas) =>
        tareas.Select(x => new TareaAsignadaRespuesta(x.IdTarea, x.TipoOperacion.NombreOperacion,
            x.Bano.CodigoInterno, x.Ubicacion == null ? null : x.Ubicacion.Direccion,
            x.FechaProgramada, x.EstadoOperacion.NombreEstado,
            x.Servicio != null ? x.Servicio.Cliente.Usuario.Nombre + " " + x.Servicio.Cliente.Usuario.Apellido : x.Solicitud == null ? null : x.Solicitud.Cliente.Usuario.Nombre + " " + x.Solicitud.Cliente.Usuario.Apellido,
            x.Solicitud != null ? x.Solicitud.TipoOperacion.NombreOperacion : x.Servicio == null || x.Servicio.SolicitudOrigen == null ? null : x.Servicio.SolicitudOrigen.TipoOperacion.NombreOperacion,
            x.Solicitud != null ? x.Solicitud.Descripcion : x.Servicio == null || x.Servicio.SolicitudOrigen == null ? null : x.Servicio.SolicitudOrigen.Descripcion,
            x.Servicio == null ? null : x.Servicio.Observaciones,
            x.ObservacionesEmpleado, x.PapelHigienicoRepuesto, x.FechaTrabajoRegistrado,
            x.IdEmpleadoRegistroTrabajo, x.EmpleadoRegistroTrabajo == null ? null : x.EmpleadoRegistroTrabajo.Nombre + " " + x.EmpleadoRegistroTrabajo.Apellido,
            x.FechaRealizada,
            (x.IdEstadoOperacion == 6 || x.IdEstadoOperacion == 7) && x.FechaTrabajoRegistrado == null,
            (x.IdEstadoOperacion == 6 || x.IdEstadoOperacion == 7) && x.FechaTrabajoRegistrado != null && x.IdEmpleadoRegistroTrabajo == x.IdEmpleado,
            x.Solicitud != null ? x.Solicitud.ObservacionesAdministrador : x.Servicio == null || x.Servicio.SolicitudOrigen == null ? null : x.Servicio.SolicitudOrigen.ObservacionesAdministrador,
            x.IdServicio, x.IdSolicitud, x.IdUbicacion, x.IdUbicacionDestino,
            x.UbicacionDestino == null ? null : x.UbicacionDestino.Direccion,
            db.TareasOperativas.Where(t => x.IdTipoOperacion == 8 && x.IdServicio != null && t.IdServicio == x.IdServicio && t.IdTarea != x.IdTarea
                && (t.IdEstadoOperacion == 7 || t.IdEstadoOperacion == 6 && !(t.IdTipoOperacion == 11 && t.BloqueaAsignacion)))
                .OrderBy(t => t.IdTarea).Select(t => new ImpedimentoRetiroRespuesta(t.IdTarea, t.TipoOperacion.NombreOperacion,
                    t.EstadoOperacion.NombreEstado, t.FechaTrabajoRegistrado != null)).ToList()));

    [HttpGet("mias/resumen")]
    public async Task<ActionResult<ResumenTareasRespuesta>> Resumen(CancellationToken ct)
    {
        // Día de trabajo de Argentina (UTC-3), independiente de la zona del servidor/teléfono.
        var hoy = reloj.GetUtcNow().ToOffset(TimeSpan.FromHours(-3)).Date;
        var inicio = new DateTimeOffset(hoy, TimeSpan.FromHours(-3));
        var fin = inicio.AddDays(1);
        var cantidad = await MisTareas().CountAsync(x => x.FechaProgramada >= inicio && x.FechaProgramada < fin, ct);
        return Ok(new ResumenTareasRespuesta(DateOnly.FromDateTime(hoy), cantidad));
    }
    [HttpGet("mias")]
    public async Task<ActionResult<List<TareaAsignadaRespuesta>>> Listar(CancellationToken ct) =>
        Ok(await Datos(MisTareas().OrderBy(x => x.FechaProgramada == null).ThenBy(x => x.FechaProgramada).ThenBy(x => x.IdTarea)).ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TareaAsignadaRespuesta>> Detalle(int id, CancellationToken ct)
    {
        // Una tarea ajena y una inexistente dan el mismo 404, sin revelar su existencia.
        var tarea = await Datos(MisTareas().Where(x => x.IdTarea == id)).SingleOrDefaultAsync(ct);
        return tarea is null ? NotFound() : Ok(tarea);
    }

    [HttpPost("{id:int}/registrar-trabajo")]
    public Task<ActionResult<TareaAsignadaRespuesta>> Registrar(int id, CancellationToken ct) => Guardar(id, false, ct);

    [HttpPost("{id:int}/finalizar")]
    public Task<ActionResult<TareaAsignadaRespuesta>> Finalizar(int id, CancellationToken ct) => Guardar(id, true, ct);

    private async Task<ActionResult<TareaAsignadaRespuesta>> Guardar(int id, bool finalizar, CancellationToken ct)
    {
        // Un único orden de escritura serializa los cierres y sus solicitudes compartidas.
        // El bloqueo pertenece a la transacción: se libera también al fallar o desconectarse.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @resultado int;
            EXEC @resultado = sys.sp_getapplock @Resource=N'BanosQuimicos.CumplimientoTareas',
                @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
            IF @resultado < 0 THROW 51000, 'No se pudo bloquear el cierre de tareas.', 1;
            """, ct);
        var empleado = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        // Revalidar dentro de la transacción, además de la política por petición.
        if (!await db.Usuarios.AnyAsync(x => x.IdUsuario == empleado && x.EstadoCuenta == "Activa" && x.Rol.NombreRol == "Empleado", ct))
            return Forbid();
        // UPDLOCK evita convertir dos bloqueos de lectura al guardar y protege reasignaciones.
        var tarea = await db.TareasOperativas.FromSqlInterpolated($"SELECT * FROM dbo.TAREAS_OPERATIVAS WITH (UPDLOCK, HOLDLOCK) WHERE IdTarea={id} AND IdEmpleado={empleado}").SingleOrDefaultAsync(ct);
        if (tarea is null) return NotFound();
        if (tarea.IdEstadoOperacion == 9) return Conflict(new { mensaje = "La tarea está cancelada." });
        if (tarea.IdEmpleadoRegistroTrabajo is { } autor && autor != empleado)
            return Conflict(new { mensaje = "El trabajo fue registrado por otro empleado. Debe revisarlo el administrador." });
        if (tarea.IdEstadoOperacion == 8)
        {
            if (tarea.FechaTrabajoRegistrado is null) return Conflict(new { mensaje = "La tarea ya estaba finalizada; no se modifica su historial." });
            await tx.CommitAsync(ct);
            return await Detalle(id, ct); // Repetición: no reemplaza fechas ni repite efectos.
        }
        if (tarea.IdEstadoOperacion is not (6 or 7)) return Conflict(new { mensaje = "Estado de tarea incompatible." });
        var ahora = reloj.GetUtcNow();
        if (ahora < tarea.FechaRegistro) return Conflict(new { mensaje = "La fecha actual es anterior a la creación de la tarea." });
        if (!finalizar)
        {
            if (tarea.FechaTrabajoRegistrado is null)
            {
                tarea.FechaTrabajoRegistrado = ahora;
                tarea.IdEmpleadoRegistroTrabajo = empleado;
            }
        }
        else
        {
            if (tarea.FechaTrabajoRegistrado is null) return Conflict(new { mensaje = "Primero registrá el trabajo." });
            if (ahora < tarea.FechaTrabajoRegistrado) return Conflict(new { mensaje = "La fecha de cierre es anterior al registro del trabajo." });
            var error = await AplicarCierre(tarea, ahora, ct);
            if (error is not null) return Conflict(new { mensaje = error });
            tarea.IdEstadoOperacion = 8; // Catálogo estable: Tarea/Realizada.
            tarea.FechaRealizada = ahora;
        }
        await db.SaveChangesAsync(ct);
        if (finalizar) await ResolverSolicitud(tarea, ahora, ct);
        await tx.CommitAsync(ct);
        return await Detalle(id, ct);
    }

    private async Task<string?> AplicarCierre(TareaOperativa tarea, DateTimeOffset ahora, CancellationToken ct)
    {
        if (tarea.IdSolicitud is { } solicitudId)
        {
            var solicitud = await db.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == solicitudId, ct);
            if (solicitud.IdEstadoOperacion != 2) return "La solicitud debe estar aprobada antes de finalizar sus trabajos.";
            if (tarea.IdServicio is not { } servicioId || !await db.Servicios.AnyAsync(x => x.IdServicio == servicioId && x.IdCliente == solicitud.IdCliente, ct))
                return "La solicitud y el servicio no corresponden al mismo cliente.";
            if (solicitud.IdServicioReferencia is { } referencia && referencia != servicioId && solicitud.IdTipoOperacion != 2)
                return "La tarea no corresponde al servicio de referencia.";
            if (tarea.IdTipoOperacion == 10 && (solicitud.IdTipoOperacion != 4 || solicitud.ModalidadCambioDireccion != "Solicitado"))
                return "La solicitud no corresponde a un traslado solicitado.";
        }
        if (tarea.IdServicio is null) return tarea.IdTipoOperacion == 11 ? null : "La tarea necesita un servicio.";
        var servicio = await db.Servicios.SingleAsync(x => x.IdServicio == tarea.IdServicio, ct);
        if (servicio.IdBano != tarea.IdBano) return "El servicio corresponde a otro baño.";
        if (tarea.IdUbicacion is { } ubicacion && !await db.UbicacionesServicio.AnyAsync(x => x.IdUbicacion == ubicacion && x.IdServicio == servicio.IdServicio, ct))
            return "La ubicación corresponde a otro servicio.";
        // El mantenimiento conserva su contexto y puede completarse después del retiro del baño.
        if (servicio.EstadoServicio == "Cancelado" || servicio.EstadoServicio == "Finalizado" && tarea.IdTipoOperacion != 11)
            return "El servicio ya no está vigente.";
        switch (tarea.IdTipoOperacion)
        {
            case 7: // Entrega: asignar no equivalía a entregar.
                if (servicio.EstadoServicio != "Pendiente de entrega" || ahora < servicio.FechaAsignacion) return "El servicio no admite la entrega.";
                if (!await db.UbicacionesServicio.AnyAsync(x => x.IdServicio == servicio.IdServicio && x.EsActual, ct))
                {
                    var entrega = await db.UbicacionesServicio.SingleOrDefaultAsync(x => x.IdUbicacion == tarea.IdUbicacion && x.IdServicio == servicio.IdServicio, ct);
                    if (entrega is null || entrega.FechaDesde is not null || entrega.FechaHasta is not null)
                        return "La entrega no tiene un destino preparado válido.";
                    // La dirección preparada se vuelve vigente al entregar, sin falsificar la fecha efectiva.
                    entrega.EsActual = true; entrega.FechaDesde = ahora;
                }
                servicio.EstadoServicio = "Entregado"; servicio.FechaEntregaReal = ahora;
                break;
            case 8: // Retiro: recién al cierre se libera la asignación vigente.
                if (servicio.EstadoServicio != "Entregado" || ahora < servicio.FechaEntregaReal) return "El servicio no admite el retiro.";
                var impedimentos = await db.TareasOperativas.Where(x => x.IdServicio == servicio.IdServicio && x.IdTarea != tarea.IdTarea
                    && (x.IdEstadoOperacion == 7 || x.IdEstadoOperacion == 6 && !(x.IdTipoOperacion == 11 && x.BloqueaAsignacion)))
                    .OrderBy(x => x.IdTarea).Select(x => new ImpedimentoRetiroRespuesta(x.IdTarea, x.TipoOperacion.NombreOperacion,
                        x.EstadoOperacion.NombreEstado, x.FechaTrabajoRegistrado != null)).ToListAsync(ct);
                if (impedimentos.Count > 0)
                    return $"Retiro del servicio {servicio.IdServicio} bloqueado por " + string.Join("; ", impedimentos.Select(x =>
                        $"tarea {x.IdTarea} {x.Tipo} ({x.Estado}{(x.TrabajoRegistrado ? ", trabajo registrado" : "")})"))
                        + ". El administrador debe revisar esos trabajos en Baños y Servicios: cancelar explícitamente pendientes sin registro que no correspondan, o completar los que deben conservarse. En curso o con trabajo registrado deben finalizarse; no se cancelan automáticamente.";
                servicio.EstadoServicio = "Finalizado"; servicio.FechaRetiroReal = ahora; servicio.FechaFinalizacion = ahora;
                break;
            case 10:
                if (servicio.EstadoServicio != "Entregado") return "El traslado requiere un servicio entregado.";
                var destino = await db.UbicacionesServicio.SingleOrDefaultAsync(x => x.IdUbicacion == tarea.IdUbicacionDestino && x.IdServicio == servicio.IdServicio, ct);
                var origen = await db.UbicacionesServicio.SingleOrDefaultAsync(x => x.IdServicio == servicio.IdServicio && x.EsActual, ct);
                if (destino is null || origen is null || origen.IdUbicacion != tarea.IdUbicacion || destino.EsActual || destino.FechaDesde != null || destino.FechaHasta != null || ahora < origen.FechaDesde)
                    return "Revisar origen y destino del traslado con el administrador.";
                var solicitudTraslado = tarea.IdSolicitud is int idTraslado ? await db.SolicitudesCliente.SingleAsync(s => s.IdSolicitud == idTraslado, ct) : null;
                var errorRevision = await BanosQuimicos.Api.Reglas.ReglasDireccion.AplicarRevision(db, solicitudTraslado,
                    servicio.IdServicio, tarea.IdTarea, origen.IdUbicacion, destino.IdUbicacion, ct);
                if (errorRevision is not null) return errorRevision;
                origen.EsActual = false; origen.FechaHasta = ahora;
                // Primero liberar el índice filtrado de ubicación actual, dentro de la transacción.
                await db.SaveChangesAsync(ct);
                destino.EsActual = true; destino.FechaDesde = ahora;
                if (tarea.IdSolicitud is { } idSolicitud)
                {
                    var solicitud = await db.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == idSolicitud, ct);
                    solicitud.FechaCambioEfectiva = ahora;
                }
                break;
            case 3 or 9:
                if (servicio.EstadoServicio != "Entregado") return "La limpieza requiere un servicio entregado.";
                break;
            case 11:
                break; // No modificar automáticamente condición física ni reposición de papel.
            default: return "Tipo de tarea no habilitado para este cierre.";
        }
        return null;
    }

    private async Task ResolverSolicitud(TareaOperativa tarea, DateTimeOffset ahora, CancellationToken ct)
    {
        if (tarea.IdSolicitud is not { } id) return;
        var solicitud = await db.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == id, ct);
        // Canceladas no satisfacen un trabajo; cero tareas tampoco resuelve una solicitud.
        if (solicitud.IdEstadoOperacion != 2 || await db.TareasOperativas.AnyAsync(x => x.IdSolicitud == id && x.IdEstadoOperacion != 8, ct)) return;
        if (solicitud.IdTipoOperacion is 1 or 2)
        {
            if (solicitud.CantidadBanos is not > 0) return;
            var servicios = await db.Servicios.Where(x => x.IdSolicitudOrigen == id).ToListAsync(ct);
            if (servicios.Count != solicitud.CantidadBanos || servicios.Any(x => x.IdCliente != solicitud.IdCliente || x.EstadoServicio != "Entregado")) return;
            foreach (var servicio in servicios)
                if (!await db.TareasOperativas.AnyAsync(x => x.IdSolicitud == id && x.IdServicio == servicio.IdServicio && x.IdTipoOperacion == 7 && x.IdEstadoOperacion == 8, ct)) return;
        }
        if (solicitud.IdTipoOperacion == 6 && !await db.Servicios.AnyAsync(x => x.IdServicio == solicitud.IdServicioReferencia && x.IdCliente == solicitud.IdCliente && x.EstadoServicio == "Finalizado", ct)) return;
        if (solicitud.IdTipoOperacion == 4 && solicitud.FechaCambioEfectiva is null) return;
        solicitud.IdEstadoOperacion = 4; solicitud.FechaResolucion = ahora;
        await db.SaveChangesAsync(ct);
    }
}
