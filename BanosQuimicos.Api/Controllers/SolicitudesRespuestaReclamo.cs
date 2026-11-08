using System.Data;
using System.Security.Claims;
using BanosQuimicos.Api.Contratos.Solicitudes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Controllers;

public partial class SolicitudesController
{
    [HttpPost("{id:int}/responder-resolver")]
    public async Task<ActionResult<SolicitudDetalleRespuesta>> ResponderResolver(int id, ResponderReclamoSolicitud datos, CancellationToken ct)
    {
        var texto = datos.Respuesta.Trim();
        if (texto.Length == 0) return BadRequest(new { mensaje = "Escribí la respuesta al reclamo." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Mismo bloqueo que asignación y cumplimiento: responder no puede omitir una tarea concurrente.
        await BanosQuimicos.Api.Reglas.ReglasDireccion.Bloquear(db, ct);
        var usuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!await db.Usuarios.AnyAsync(u => u.IdUsuario == usuario && u.EstadoCuenta == "Activa" && u.Rol.NombreRol == "Administrador", ct)) return Forbid();
        var solicitud = await db.SolicitudesCliente.SingleOrDefaultAsync(s => s.IdSolicitud == id, ct);
        if (solicitud is null) return NotFound();
        if (solicitud.IdTipoOperacion != 5) return Conflict(new { mensaje = "Responder y resolver corresponde únicamente a Reclamos." });
        var impedimentos = await db.TareasOperativas.Where(t => t.IdSolicitud == id && (t.IdEstadoOperacion == 6 || t.IdEstadoOperacion == 7
                || t.FechaTrabajoRegistrado != null && t.FechaRealizada == null))
            .OrderBy(t => t.IdTarea).Select(t => "Tarea " + t.IdTarea + ": " + t.TipoOperacion.NombreOperacion + " (" + t.EstadoOperacion.NombreEstado + ")").ToListAsync(ct);
        if (impedimentos.Count > 0) return Conflict(new { mensaje = "No se puede resolver por respuesta. Deben finalizarse los trabajos vinculados: " + string.Join("; ", impedimentos) });
        // Un reclamo solo tiene un cierre: el mismo contenido recupera su resultado, nunca reescribe historial.
        if (solicitud.IdEstadoOperacion == 4 && solicitud.FechaResolucion != null && solicitud.ObservacionesAdministrador == texto)
        { await tx.CommitAsync(ct); return await Detalle(id, ct); }
        if (solicitud.IdEstadoOperacion is not (1 or 2) || solicitud.IdEstadoOperacion != datos.IdEstadoAnterior)
            return Conflict(new { mensaje = "El reclamo cambió o ya está cerrado. Consultá su estado actual; no se reemplaza una resolución anterior." });
        if (!string.IsNullOrWhiteSpace(solicitud.ObservacionesAdministrador) && solicitud.ObservacionesAdministrador != texto)
            return Conflict(new { mensaje = "El reclamo ya tiene una explicación administrativa. Se conserva su contenido; no se sobrescribe desde este cierre." });
        var ahora = reloj.GetUtcNow();
        if (ahora < solicitud.FechaSolicitud) return Conflict(new { mensaje = "La fecha actual es anterior a la creación del reclamo." });
        solicitud.ObservacionesAdministrador = texto;
        solicitud.IdEstadoOperacion = 4; // Solicitud/Resuelta, sin tarea ficticia ni aprobación intermedia.
        solicitud.FechaResolucion = ahora;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return await Detalle(id, ct);
    }
}
