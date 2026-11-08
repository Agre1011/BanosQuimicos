using System.Data;
using System.Security.Claims;
using System.Text.Json;
using BanosQuimicos.Api.Contratos.Solicitudes;
using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Modelos;
using BanosQuimicos.Api.Reglas;
using BanosQuimicos.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Controllers;

[ApiController]
[Route("api/solicitudes/{id:int}/direccion")]
[Authorize(Policy = Politicas.Administrador)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DireccionSolicitudesController(BanosQuimicosDbContext db, TimeProvider reloj) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RevisionDireccionRespuesta>> Consultar(int id, CancellationToken ct)
    {
        var s = await db.SolicitudesCliente.AsNoTracking().Include(s => s.EstadoOperacion).SingleOrDefaultAsync(s => s.IdSolicitud == id && s.IdTipoOperacion == 4, ct);
        if (s?.IdServicioReferencia is not int servicio) return NotFound();
        var revision = ReglasDireccion.Leer(s);
        var origen = await db.UbicacionesServicio.AsNoTracking().SingleOrDefaultAsync(u => u.IdServicio == servicio && (revision != null ? u.IdUbicacion == revision.Origen : u.EsActual), ct);
        if (origen is null) return Conflict(new { mensaje = "Revisar la ubicación de origen del servicio." });
        var traslado = await db.TareasOperativas.Where(t => t.IdSolicitud == id && t.IdTipoOperacion == 10 && (t.IdEstadoOperacion == 6 || t.IdEstadoOperacion == 7)).Select(t => (int?)t.IdTarea).FirstOrDefaultAsync(ct);
        var tareas = await db.TareasOperativas.AsNoTracking().Where(t => t.IdServicio == servicio && (traslado == null || t.IdTarea != traslado)
            && (t.IdEstadoOperacion == 6 || t.IdEstadoOperacion == 7)).OrderBy(t => t.IdTarea)
            .Select(t => new TareaRevisionRespuesta(t.IdTarea, t.TipoOperacion.NombreOperacion, t.EstadoOperacion.NombreEstado,
                t.IdUbicacion, t.Ubicacion == null ? null : t.Ubicacion.Direccion, t.IdEstadoOperacion == 7 || t.FechaTrabajoRegistrado != null)).ToListAsync(ct);
        return Ok(new RevisionDireccionRespuesta(id, s.IdEstadoOperacion, s.EstadoOperacion.NombreEstado, s.ModalidadCambioDireccion!,
            origen.IdUbicacion, origen.Direccion, s.NuevaDireccion!, s.FechaCambioEfectiva, revision?.Fecha != null, tareas));
    }

    [HttpPost]
    public async Task<ActionResult<RevisionDireccionRespuesta>> Revisar(int id, RevisarDireccionSolicitud datos, CancellationToken ct)
    {
        if (datos.ClaveRevision == Guid.Empty) return BadRequest();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await ReglasDireccion.Bloquear(db, ct);
        var usuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!await db.Usuarios.AnyAsync(u => u.IdUsuario == usuario && u.EstadoCuenta == "Activa" && u.Rol.NombreRol == "Administrador", ct)) return Forbid();
        var s = await db.SolicitudesCliente.SingleOrDefaultAsync(s => s.IdSolicitud == id && s.IdTipoOperacion == 4, ct);
        if (s?.IdServicioReferencia is not int servicio) return NotFound();
        var previa = ReglasDireccion.Leer(s);
        var decisiones = datos.Decisiones.OrderBy(d => d.IdTarea).ToList();
        var explicacion = string.IsNullOrWhiteSpace(datos.Explicacion) ? null : datos.Explicacion.Trim();
        if (previa?.Clave == datos.ClaveRevision)
        {
            if (previa.Origen != datos.IdUbicacionEsperada || previa.Explicacion != explicacion
                || JsonSerializer.Serialize(previa.Decisiones) != JsonSerializer.Serialize(decisiones))
                return Conflict(new { mensaje = "La clave corresponde a otra revisión." });
            await tx.CommitAsync(ct); return await Consultar(id, ct);
        }
        if (s.IdEstadoOperacion != datos.IdEstadoAnterior || s.IdEstadoOperacion is not (1 or 2)
            || !await db.Servicios.AnyAsync(v => v.IdServicio == servicio && v.IdCliente == s.IdCliente && v.EstadoServicio == "Entregado", ct))
            return Conflict(new { mensaje = "El estado de la solicitud o servicio cambió." });
        var conflicto = await ReglasDireccion.Conflicto(db, servicio, id, ct);
        if (conflicto is not null) return Conflict(new { mensaje = "Operación incompatible: " + conflicto });
        var origen = await db.UbicacionesServicio.SingleOrDefaultAsync(u => u.IdServicio == servicio && u.EsActual, ct);
        if (origen is null || origen.IdUbicacion != datos.IdUbicacionEsperada || previa != null && previa.Origen != origen.IdUbicacion)
            return Conflict(new { mensaje = "La ubicación actual cambió." });
        var traslado = await db.TareasOperativas.SingleOrDefaultAsync(t => t.IdSolicitud == id && t.IdTipoOperacion == 10 && (t.IdEstadoOperacion == 6 || t.IdEstadoOperacion == 7), ct);
        if (s.ModalidadCambioDireccion == "Solicitado" && traslado?.IdUbicacionDestino is null)
            return Conflict(new { mensaje = "Primero aprobar y asignar el traslado solicitado." });
        if (s.ModalidadCambioDireccion == "Informado" && await db.TareasOperativas.AnyAsync(t => t.IdSolicitud == id, ct))
            return Conflict(new { mensaje = "El aviso informado no debe tener una tarea física. Revisar el historial." });
        var pendientes = await ReglasDireccion.Pendientes(db, servicio, traslado?.IdTarea, ct);
        var error = ReglasDireccion.ValidarDecisiones(pendientes, decisiones);
        if (error is not null) return Conflict(new { mensaje = error });
        var ahora = reloj.GetUtcNow(); int destino;
        if (s.ModalidadCambioDireccion == "Informado")
        {
            if (s.FechaCambioEfectiva is not { } efectiva || efectiva > ahora || efectiva < origen.FechaDesde || string.IsNullOrWhiteSpace(s.NuevaDireccion))
                return Conflict(new { mensaje = "La fecha efectiva no puede ser futura ni anterior a la vigencia del origen." });
            var nueva = new UbicacionServicio { IdServicio = servicio, Direccion = s.NuevaDireccion,
                FechaRegistro = ahora, FechaValidacion = ahora, FechaDesde = efectiva, EsActual = false };
            db.UbicacionesServicio.Add(nueva); await db.SaveChangesAsync(ct); destino = nueva.IdUbicacion;
            origen.EsActual = false; origen.FechaHasta = efectiva; await db.SaveChangesAsync(ct);
            nueva.EsActual = true;
            foreach (var tarea in pendientes)
                if (decisiones.Single(d => d.IdTarea == tarea.IdTarea).Mover) tarea.IdUbicacion = destino;
            s.IdEstadoOperacion = 4; s.FechaValidacion = ahora; s.FechaResolucion = ahora;
        }
        else if (s.ModalidadCambioDireccion == "Solicitado") destino = traslado!.IdUbicacionDestino!.Value;
        else return Conflict(new { mensaje = "Modalidad de cambio no reconocida." });
        ReglasDireccion.Guardar(s, new(origen.IdUbicacion, destino, datos.ClaveRevision, ahora, decisiones, explicacion));
        if (explicacion is not null) s.ObservacionesAdministrador = explicacion;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return await Consultar(id, ct);
    }
}
