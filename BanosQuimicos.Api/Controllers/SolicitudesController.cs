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
public partial class SolicitudesController(BanosQuimicosDbContext db, TimeProvider reloj) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SolicitudResumenRespuesta>>> Listar(CancellationToken ct) =>
        Ok(await db.SolicitudesCliente.AsNoTracking().OrderByDescending(x => x.FechaSolicitud).ThenByDescending(x => x.IdSolicitud)
            .Select(x => new SolicitudResumenRespuesta(x.IdSolicitud, x.Cliente.Usuario.Nombre + " " + x.Cliente.Usuario.Apellido,
                x.TipoOperacion.NombreOperacion, x.EstadoOperacion.NombreEstado, x.FechaSolicitud)).ToListAsync(ct));

    [HttpGet("pagina")]
    public async Task<IActionResult> Pagina(CancellationToken ct, [FromQuery, System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] int pagina = 1) =>
        Ok(await BanosQuimicos.Api.Contratos.ConsultaPaginada.Leer(db.SolicitudesCliente.AsNoTracking()
            .OrderByDescending(x => x.FechaSolicitud).ThenByDescending(x => x.IdSolicitud)
            .Select(x => new SolicitudResumenRespuesta(x.IdSolicitud, x.Cliente.Usuario.Nombre + " " + x.Cliente.Usuario.Apellido,
                x.TipoOperacion.NombreOperacion, x.EstadoOperacion.NombreEstado, x.FechaSolicitud)), pagina, ct));

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
                        t.Empleado == null ? null : t.Empleado.Nombre + " " + t.Empleado.Apellido, t.FechaProgramada, t.Bano.CodigoInterno, t.IdServicio)).ToList(),
                db.UbicacionesServicio.Where(u => u.IdServicio == x.IdServicioReferencia && u.EsActual).Select(u => (int?)u.IdUbicacion).FirstOrDefault(),
                db.Servicios.Where(s => s.IdSolicitudOrigen == x.IdSolicitud).OrderBy(s => s.IdServicio)
                    .Select(s => new ServicioSolicitudRespuesta(s.IdServicio, s.IdBano, s.Bano.CodigoInterno,
                        s.EstadoServicio, s.TipoUso, s.CantidadLimpiezasSemana,
                        db.UbicacionesServicio.Where(u => u.IdServicio == s.IdServicio).OrderByDescending(u => u.EsActual).ThenByDescending(u => u.IdUbicacion)
                            .Select(u => u.Direccion).FirstOrDefault())).ToList()))
            .SingleOrDefaultAsync(ct);
        return detalle is null ? NotFound() : Ok(detalle);
    }

    private IQueryable<BanoQuimico> Disponibles(int tipo) => db.BanosQuimicos.Where(b => b.FechaBaja == null && b.IdTipoBano == tipo && b.EstadoBano.NombreEstado == "Apto"
        && !db.Servicios.Any(s => s.IdBano == b.IdBano && (s.EstadoServicio == "Pendiente de entrega" || s.EstadoServicio == "Entregado"))
        && !db.TareasOperativas.Any(t => t.IdBano == b.IdBano && t.BloqueaAsignacion && (t.IdEstadoOperacion == 6 || t.IdEstadoOperacion == 7)));

    [HttpGet("{id:int}/banos-disponibles")]
    public async Task<ActionResult<List<BanoDisponibleRespuesta>>> BanosDisponibles(int id, CancellationToken ct)
    {
        var s = await db.SolicitudesCliente.AsNoTracking().SingleOrDefaultAsync(x => x.IdSolicitud == id, ct);
        if (s is null) return NotFound();
        if (s.IdTipoOperacion != 1 || s.IdEstadoOperacion is not (1 or 2) || s.IdTipoBanoSolicitado is null)
            return Conflict(new { mensaje = "La solicitud no admite selección de baños para servicio nuevo." });
        return Ok(await Disponibles(s.IdTipoBanoSolicitado.Value).AsNoTracking().OrderBy(b => b.CodigoInterno).ThenBy(b => b.IdBano)
            .Select(b => new BanoDisponibleRespuesta(b.IdBano, b.CodigoInterno, b.TipoBano.NombreTipo)).ToListAsync(ct));
    }

    [HttpPost("{id:int}/asignar-banos")]
    public async Task<ActionResult<SolicitudDetalleRespuesta>> AsignarBanos(int id, AsignarBanosSolicitud datos, CancellationToken ct)
    {
        if (datos.ClaveProgramacion == Guid.Empty || datos.Fecha == default || datos.IdBanos.Any(x => x <= 0) || datos.IdBanos.Distinct().Count() != datos.IdBanos.Count)
            return BadRequest(new { mensaje = "Indicar una clave, fecha y baños distintos válidos." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @r int;
            EXEC @r=sys.sp_getapplock @Resource=N'BanosQuimicos.CumplimientoTareas',
                @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
            IF @r<0 THROW 51000, 'No se pudo bloquear la asignación de baños.', 1;
            """, ct);
        var admin = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!await db.Usuarios.AnyAsync(u => u.IdUsuario == admin && u.EstadoCuenta == "Activa" && u.Rol.NombreRol == "Administrador", ct)) return Forbid();
        var solicitud = await db.SolicitudesCliente.SingleOrDefaultAsync(s => s.IdSolicitud == id, ct);
        if (solicitud is null) return NotFound();
        if (solicitud.IdTipoOperacion != 1 || solicitud.IdServicioReferencia is not null)
            return Conflict(new { mensaje = "Esta operación solo atiende un pedido de servicio nuevo, sin servicio de referencia." });
        if (solicitud.CantidadBanos is not > 0 || solicitud.IdTipoBanoSolicitado is null || solicitud.TipoUsoSolicitado is not ("Obra" or "Evento")
            || string.IsNullOrWhiteSpace(solicitud.DireccionSolicitada) || solicitud.FechaDeseada is null)
            return Conflict(new { mensaje = "Faltan datos necesarios de la solicitud; no se completan con valores inventados." });
        if (datos.IdBanos.Count != solicitud.CantidadBanos) return BadRequest(new { mensaje = "Seleccionar exactamente la cantidad solicitada." });
        var ids = datos.IdBanos.Order().ToArray();
        var fecha = new DateTimeOffset(datos.Fecha.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-3));
        var previa = await db.TareasOperativas.SingleOrDefaultAsync(t => t.ClaveProgramacion == datos.ClaveProgramacion, ct);
        if (previa is not null)
        {
            // La primera entrega conserva la clave del pedido. Las otras tienen sus propias claves.
            // Reconocer el lote persistido permite reintentar sin volver a reservar unidades.
            var servicios = await db.Servicios.Where(s => s.IdSolicitudOrigen == id).ToListAsync(ct);
            var entregas = await db.TareasOperativas.Where(t => t.IdSolicitud == id && t.IdTipoOperacion == 7).ToListAsync(ct);
            if (previa.IdSolicitud != id || previa.IdTipoOperacion != 7 || solicitud.IdEstadoOperacion is not (2 or 4)
                || servicios.Count != ids.Length || !servicios.Select(s => s.IdBano).Order().SequenceEqual(ids)
                || servicios.Any(s => s.IdCliente != solicitud.IdCliente || s.CantidadLimpiezasSemana != datos.CantidadLimpiezasSemana || s.TipoUso != solicitud.TipoUsoSolicitado || s.FechaEntregaProgramada != fecha)
                || entregas.Count != ids.Length || entregas.Any(t => t.IdEmpleado != datos.IdEmpleado || t.FechaProgramada != fecha || t.IdEstadoOperacion == 9)
                || servicios.Any(s => entregas.Count(t => t.IdServicio == s.IdServicio && t.IdBano == s.IdBano) != 1))
                return Conflict(new { mensaje = "La clave corresponde a otro pedido o a una asignación distinta." });
            await tx.CommitAsync(ct); return await Detalle(id, ct);
        }
        if (solicitud.IdEstadoOperacion != datos.IdEstadoAnterior || solicitud.IdEstadoOperacion is not (1 or 2))
            return Conflict(new { mensaje = "El estado cambió. Consultá nuevamente la solicitud." });
        if (await db.Servicios.AnyAsync(s => s.IdSolicitudOrigen == id, ct) || await db.TareasOperativas.AnyAsync(t => t.IdSolicitud == id, ct))
            return Conflict(new { mensaje = "El pedido ya tiene servicios o trabajos. Se conserva su historial, sin duplicarlo ni completarlo parcialmente." });
        if (!await db.Usuarios.AnyAsync(u => u.IdUsuario == datos.IdEmpleado && u.EstadoCuenta == "Activa" && u.Rol.NombreRol == "Empleado", ct))
            return Conflict(new { mensaje = "Elegir un empleado activo para las entregas." });
        // Tipo físico solicitado y uso son conceptos separados: Obra puede utilizarse en Evento.
        if (await Disponibles(solicitud.IdTipoBanoSolicitado.Value).CountAsync(b => ids.Contains(b.IdBano), ct) != ids.Length)
            return Conflict(new { mensaje = "Algún baño dejó de estar disponible, no es Apto o no corresponde al tipo solicitado. No se guardó el pedido parcialmente." });
        var ahora = reloj.GetUtcNow();
        if (ahora < solicitud.FechaSolicitud) return Conflict(new { mensaje = "La fecha de solicitud es posterior a la actual." });
        for (var i = 0; i < ids.Length; i++)
        {
            var servicio = new Servicio { IdSolicitudOrigen = id, IdCliente = solicitud.IdCliente, IdBano = ids[i],
                TipoUso = solicitud.TipoUsoSolicitado, CantidadLimpiezasSemana = datos.CantidadLimpiezasSemana!.Value,
                EstadoServicio = "Pendiente de entrega", FechaAsignacion = ahora, FechaEntregaProgramada = fecha };
            var destino = new UbicacionServicio { Servicio = servicio, Direccion = solicitud.DireccionSolicitada,
                FechaRegistro = ahora, EsActual = false };
            db.TareasOperativas.Add(new TareaOperativa { Servicio = servicio, IdBano = ids[i], IdSolicitud = id,
                IdTipoOperacion = 7, IdEstadoOperacion = 6, IdEmpleado = datos.IdEmpleado, Ubicacion = destino,
                FechaRegistro = ahora, FechaProgramada = fecha, ClaveProgramacion = i == 0 ? datos.ClaveProgramacion : Guid.NewGuid() });
        }
        solicitud.IdEstadoOperacion = 2; // Aprobar no equivale a entregar ni resolver.
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return await Detalle(id, ct);
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
        if (await db.BanosQuimicos.AnyAsync(b => b.IdBano == servicio.IdBano && b.FechaBaja != null, ct))
            return Conflict(new { mensaje = "El baño está dado de baja del inventario y no admite trabajos nuevos." });
        if (tipo is 8 or 10)
        {
            var conflicto = await BanosQuimicos.Api.Reglas.ReglasDireccion.Conflicto(db, servicio.IdServicio, id, ct, tipo == 8);
            if (conflicto is not null) return Conflict(new { mensaje = "Operación incompatible: " + conflicto });
        }
        if (!await db.UbicacionesServicio.AnyAsync(x => x.IdUbicacion == datos.IdUbicacionEsperada && x.IdServicio == servicio.IdServicio && x.EsActual, ct))
            return Conflict(new { mensaje = "La ubicación actual cambió o no está disponible." });
        var ahora = reloj.GetUtcNow();
        if (ahora < solicitud.FechaSolicitud) return Conflict(new { mensaje = "La solicitud tiene una fecha posterior a la fecha actual." });
        if (tipo == 8 && await db.TareasOperativas.AnyAsync(x => x.IdServicio == servicio.IdServicio && x.IdTipoOperacion == 8 && x.IdEstadoOperacion != 9, ct))
            return Conflict(new { mensaje = "Este servicio ya tiene un retiro; no se duplica desde otra solicitud." });
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
