using BanosQuimicos.Api.Contratos.Servicios;
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
[Route("api/servicios")]
[Authorize(Policy = Politicas.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ServiciosController(BanosQuimicosDbContext db, TimeProvider reloj) : ControllerBase
{
    private IQueryable<ServicioGestionRespuesta> Datos(int? id = null) =>
        from s in db.Servicios.AsNoTracking()
        where id == null || s.IdServicio == id
        let mantenimiento = db.TareasOperativas.Any(t => t.IdBano == s.IdBano && t.BloqueaAsignacion && (t.IdEstadoOperacion == 6 || t.IdEstadoOperacion == 7))
        orderby s.Bano.CodigoInterno, s.IdServicio descending
        select new ServicioGestionRespuesta(s.IdServicio, s.IdBano, s.Bano.CodigoInterno, s.Bano.TipoBano.NombreTipo, s.TipoUso,
            s.Cliente.Usuario.Nombre + " " + s.Cliente.Usuario.Apellido,
            db.UbicacionesServicio.Where(u => u.IdServicio == s.IdServicio).OrderByDescending(u => u.EsActual).ThenByDescending(u => u.IdUbicacion).Select(u => u.Direccion).FirstOrDefault(),
            s.CantidadLimpiezasSemana, s.EstadoServicio,
            s.Bano.ResponsableLimpieza == null ? null : s.Bano.ResponsableLimpieza.Nombre + " " + s.Bano.ResponsableLimpieza.Apellido,
            s.Bano.EstadoBano.NombreEstado, mantenimiento,
            s.Bano.FechaBaja == null && s.Bano.EstadoBano.NombreEstado == "Apto" && !mantenimiento && !db.Servicios.Any(o => o.IdBano == s.IdBano && (o.EstadoServicio == "Pendiente de entrega" || o.EstadoServicio == "Entregado")),
            db.UbicacionesServicio.Where(u => u.IdServicio == s.IdServicio && u.EsActual).Select(u => (int?)u.IdUbicacion).FirstOrDefault(),
            s.FechaEntregaReal, s.FechaRetiroReal, s.FechaFinalizacion,
            db.TareasOperativas.Where(t => t.IdServicio == s.IdServicio).OrderBy(t => t.IdTarea)
                .Select(t => new TrabajoServicioRespuesta(t.IdTarea, t.IdTipoOperacion, t.TipoOperacion.NombreOperacion,
                    t.IdEstadoOperacion, t.EstadoOperacion.NombreEstado, t.Empleado == null ? null : t.Empleado.Nombre + " " + t.Empleado.Apellido,
                    t.FechaProgramada, t.IdSolicitud, t.BloqueaAsignacion, t.FechaTrabajoRegistrado)).ToList(),
            db.SolicitudesCliente.Where(r => r.IdServicioReferencia == s.IdServicio && r.IdTipoOperacion == 6)
                .OrderBy(r => r.IdSolicitud).Select(r => new BajaServicioRespuesta(r.IdSolicitud, r.IdEstadoOperacion, r.EstadoOperacion.NombreEstado, r.Descripcion)).ToList());

    [HttpGet]
    public async Task<ActionResult<List<ServicioGestionRespuesta>>> Listar(CancellationToken ct) => Ok(await Datos().ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServicioGestionRespuesta>> Detalle(int id, CancellationToken ct)
    {
        var servicio = await Datos(id).SingleOrDefaultAsync(ct);
        return servicio is null ? NotFound() : Ok(servicio);
    }

    [HttpPost("{id:int}/retiro")]
    public async Task<ActionResult<ServicioGestionRespuesta>> Retiro(int id, ProgramarRetiroSolicitud datos, CancellationToken ct)
    {
        if (datos.ClaveProgramacion == Guid.Empty || datos.Fecha == default || datos.IdSolicitudBaja.HasValue != datos.IdEstadoSolicitudAnterior.HasValue)
            return BadRequest(new { mensaje = "Indicar fecha, clave y estado de la solicitud elegida cuando corresponda." });
        await using var tx = await Bloquear(ct);
        if (!await AdminActivo(ct)) return Forbid();
        var servicio = await db.Servicios.SingleOrDefaultAsync(x => x.IdServicio == id, ct);
        if (servicio is null) return NotFound();
        var fecha = new DateTimeOffset(datos.Fecha.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-3));
        var previa = await db.TareasOperativas.SingleOrDefaultAsync(x => x.ClaveProgramacion == datos.ClaveProgramacion, ct);
        if (previa is not null)
        {
            if (previa.IdServicio != id || previa.IdTipoOperacion != 8 || previa.IdEmpleado != datos.IdEmpleado || previa.IdUbicacion != datos.IdUbicacionEsperada
                || previa.IdSolicitud != datos.IdSolicitudBaja || previa.FechaProgramada != fecha || previa.IdEstadoOperacion == 9)
                return Conflict(new { mensaje = "La clave corresponde a otro retiro o a una tarea cancelada." });
            await tx.CommitAsync(ct); return await Detalle(id, ct);
        }
        if (servicio.EstadoServicio != "Entregado") return Conflict(new { mensaje = "Solo se retira un servicio entregado; no se elimina ni cancela desde esta acción." });
        if (await db.BanosQuimicos.AnyAsync(b => b.IdBano == servicio.IdBano && b.FechaBaja != null, ct))
            return Conflict(new { mensaje = "El baño está dado de baja del inventario y no admite trabajos nuevos." });
        var conflictoDireccion = await BanosQuimicos.Api.Reglas.ReglasDireccion.Conflicto(db, id, datos.IdSolicitudBaja, ct, true);
        if (conflictoDireccion is not null) return Conflict(new { mensaje = "Operación incompatible: " + conflictoDireccion });
        if (await db.TareasOperativas.AnyAsync(x => x.IdServicio == id && x.IdTipoOperacion == 8 && x.IdEstadoOperacion != 9, ct))
            return Conflict(new { mensaje = "El servicio ya tiene un retiro. Consultá su estado, no se crea otro." });
        if (!await db.Usuarios.AnyAsync(x => x.IdUsuario == datos.IdEmpleado && x.EstadoCuenta == "Activa" && x.Rol.NombreRol == "Empleado", ct))
            return Conflict(new { mensaje = "Elegir un empleado activo." });
        if (!await db.UbicacionesServicio.AnyAsync(x => x.IdUbicacion == datos.IdUbicacionEsperada && x.IdServicio == id && x.EsActual, ct))
            return Conflict(new { mensaje = "La ubicación actual cambió o no está disponible." });
        if (datos.IdSolicitudBaja is { } solicitudId)
        {
            var solicitud = await db.SolicitudesCliente.SingleOrDefaultAsync(x => x.IdSolicitud == solicitudId, ct);
            if (solicitud is null || solicitud.IdServicioReferencia != id || solicitud.IdCliente != servicio.IdCliente || solicitud.IdTipoOperacion != 6
                || solicitud.IdEstadoOperacion != datos.IdEstadoSolicitudAnterior || solicitud.IdEstadoOperacion is not (1 or 2))
                return Conflict(new { mensaje = "La solicitud de baja no corresponde o cambió de estado." });
            if (reloj.GetUtcNow() < solicitud.FechaSolicitud) return Conflict(new { mensaje = "La fecha actual es anterior a la solicitud." });
            solicitud.IdEstadoOperacion = 2; // Aprobar la baja no significa resolverla ni liberar el baño.
        }
        else if (await db.SolicitudesCliente.AnyAsync(x => x.IdServicioReferencia == id && x.IdTipoOperacion == 6 && (x.IdEstadoOperacion == 1 || x.IdEstadoOperacion == 2), ct))
            return Conflict(new { mensaje = "Existen solicitudes de baja. Elegí explícitamente cuál se atenderá." });
        db.TareasOperativas.Add(new TareaOperativa { IdServicio = id, IdBano = servicio.IdBano, IdSolicitud = datos.IdSolicitudBaja,
            IdTipoOperacion = 8, IdEstadoOperacion = 6, IdEmpleado = datos.IdEmpleado, IdUbicacion = datos.IdUbicacionEsperada,
            FechaProgramada = fecha, FechaRegistro = reloj.GetUtcNow(), ClaveProgramacion = datos.ClaveProgramacion });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return await Detalle(id, ct);
    }

    [HttpPost("{id:int}/resolver-pendientes")]
    public async Task<ActionResult<ServicioGestionRespuesta>> ResolverPendientes(int id, ResolverPendientesSolicitud datos, CancellationToken ct)
    {
        var ids = datos.IdCancelar.Concat(datos.IdConservar).ToArray();
        if (ids.Any(x => x <= 0) || ids.Distinct().Count() != ids.Length) return BadRequest(new { mensaje = "Cada tarea debe tener una sola decisión." });
        await using var tx = await Bloquear(ct);
        if (!await AdminActivo(ct)) return Forbid();
        if (!await db.Servicios.AnyAsync(x => x.IdServicio == id && x.EstadoServicio == "Entregado", ct)
            || !await db.TareasOperativas.AnyAsync(x => x.IdServicio == id && x.IdTipoOperacion == 8 && (x.IdEstadoOperacion == 6 || x.IdEstadoOperacion == 7), ct))
            return Conflict(new { mensaje = "Se revisan pendientes de un servicio entregado con retiro programado." });
        var trabajos = await db.TareasOperativas.Where(x => x.IdServicio == id && x.IdTipoOperacion != 8 && ids.Contains(x.IdTarea)).ToListAsync(ct);
        if (trabajos.Count != ids.Length || await db.TareasOperativas.AnyAsync(x => x.IdServicio == id && x.IdTipoOperacion != 8
            && (x.IdEstadoOperacion == 6 || x.IdEstadoOperacion == 7) && !ids.Contains(x.IdTarea), ct))
            return Conflict(new { mensaje = "Las tareas pendientes cambiaron. Consultá nuevamente antes de decidir." });
        if (trabajos.Any(x => datos.IdCancelar.Contains(x.IdTarea) && (x.IdEstadoOperacion is not (6 or 9) || x.BloqueaAsignacion || x.FechaTrabajoRegistrado is not null))
            || trabajos.Any(x => datos.IdConservar.Contains(x.IdTarea) && x.IdEstadoOperacion == 9))
            return Conflict(new { mensaje = "No se cancelan trabajos En curso, ya registrados ni mantenimientos bloqueantes; deben conservarse o completarse según su estado." });
        var ahora = reloj.GetUtcNow();
        if (trabajos.Any(x => datos.IdCancelar.Contains(x.IdTarea) && x.IdEstadoOperacion == 6 && x.FechaRegistro > ahora))
            return Conflict(new { mensaje = "La fecha actual es anterior al registro de un trabajo seleccionado." });
        foreach (var trabajo in trabajos.Where(x => datos.IdCancelar.Contains(x.IdTarea) && x.IdEstadoOperacion == 6))
        {
            trabajo.IdEstadoOperacion = 9; trabajo.FechaCancelacion = ahora;
            // Conservar empleado, contexto, observaciones y solicitud. Cancelar no la resuelve.
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return await Detalle(id, ct);
    }

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
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @r int;
                EXEC @r=sys.sp_getapplock @Resource=N'BanosQuimicos.CumplimientoTareas', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
                IF @r<0 THROW 51000, 'No se pudo bloquear la baja del servicio.', 1;
                """, ct);
            return tx;
        }
        catch { await tx.DisposeAsync(); throw; }
    }
}
