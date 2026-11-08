using System.Text.Json;
using BanosQuimicos.Api.Contratos.Solicitudes;
using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Reglas;

public record RevisionUbicacion(int Origen, int Destino, Guid Clave, DateTimeOffset? Fecha,
    List<DecisionUbicacionSolicitud>? Decisiones, string? Explicacion);

public static class ReglasDireccion
{
    public static RevisionUbicacion? Leer(SolicitudCliente s) => s.RevisionUbicacionJson is null
        ? null : JsonSerializer.Deserialize<RevisionUbicacion>(s.RevisionUbicacionJson);
    public static void Guardar(SolicitudCliente s, RevisionUbicacion revision) => s.RevisionUbicacionJson = JsonSerializer.Serialize(revision);
    public static Task Bloquear(BanosQuimicosDbContext db, CancellationToken ct) => db.Database.ExecuteSqlRawAsync("""
        DECLARE @r int;
        EXEC @r=sys.sp_getapplock @Resource=N'BanosQuimicos.CumplimientoTareas',
            @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
        IF @r<0 THROW 51000, 'No se pudo bloquear la operación.', 1;
        """, ct);

    public static async Task<string?> Conflicto(BanosQuimicosDbContext db, int servicio, int? excluirSolicitud, CancellationToken ct, bool retiroConBaja = false)
    {
        var solicitud = await db.SolicitudesCliente.AsNoTracking().Where(s => s.IdServicioReferencia == servicio
            && s.IdSolicitud != excluirSolicitud && (s.IdTipoOperacion == 4 || s.IdTipoOperacion == 6 && !retiroConBaja)
            && (s.IdEstadoOperacion == 1 || s.IdEstadoOperacion == 2))
            .OrderBy(s => s.IdSolicitud).Select(s => new { s.IdSolicitud, Tipo = s.TipoOperacion.NombreOperacion, Estado = s.EstadoOperacion.NombreEstado }).FirstOrDefaultAsync(ct);
        if (solicitud is not null) return $"Solicitud {solicitud.IdSolicitud}: {solicitud.Tipo} ({solicitud.Estado}).";
        var tarea = await db.TareasOperativas.AsNoTracking().Where(t => t.IdServicio == servicio
            && (t.IdTipoOperacion == 8 || t.IdTipoOperacion == 10) && (t.IdEstadoOperacion == 6 || t.IdEstadoOperacion == 7)
            && (excluirSolicitud == null || t.IdSolicitud != excluirSolicitud || t.IdSolicitud == null))
            .OrderBy(t => t.IdTarea).Select(t => new { t.IdTarea, Tipo = t.TipoOperacion.NombreOperacion, Estado = t.EstadoOperacion.NombreEstado }).FirstOrDefaultAsync(ct);
        return tarea is null ? null : $"Tarea {tarea.IdTarea}: {tarea.Tipo} ({tarea.Estado}).";
    }

    public static Task<List<TareaOperativa>> Pendientes(BanosQuimicosDbContext db, int servicio, int? traslado, CancellationToken ct) =>
        db.TareasOperativas.Where(t => t.IdServicio == servicio && (traslado == null || t.IdTarea != traslado)
            && (t.IdEstadoOperacion == 6 || t.IdEstadoOperacion == 7)).OrderBy(t => t.IdTarea).ToListAsync(ct);

    public static string? ValidarDecisiones(List<TareaOperativa> tareas, List<DecisionUbicacionSolicitud> decisiones)
    {
        var bloqueantes = tareas.Where(t => t.IdEstadoOperacion == 7 || t.FechaTrabajoRegistrado != null).Select(t => t.IdTarea).ToArray();
        if (bloqueantes.Length > 0) return "Resolver antes las tareas En curso o con trabajo registrado: " + string.Join(", ", bloqueantes) + ".";
        if (decisiones.Select(d => d.IdTarea).Distinct().Count() != decisiones.Count || tareas.Count != decisiones.Count
            || tareas.Any(t => !decisiones.Any(d => d.IdTarea == t.IdTarea && d.IdUbicacion == t.IdUbicacion)))
            return "Las tareas o sus ubicaciones cambiaron. Revisarlas nuevamente antes de continuar.";
        return null;
    }
    public static async Task<string?> AplicarRevision(BanosQuimicosDbContext db, SolicitudCliente? solicitud, int servicio, int traslado, int origen, int destino, CancellationToken ct)
    {
        var pendientes = await Pendientes(db, servicio, traslado, ct);
        if (pendientes.Count == 0) return null; // Sin trabajos pendientes no hay decisiones de ubicación que tomar.
        var revision = solicitud is null ? null : Leer(solicitud);
        if (revision?.Fecha is null || revision.Origen != origen || revision.Destino != destino || revision.Decisiones is null)
            return "El administrador debe revisar la ubicación de las tareas pendientes antes de finalizar el traslado.";
        var error = ValidarDecisiones(pendientes, revision.Decisiones);
        if (error is not null) return error;
        foreach (var tarea in pendientes)
            if (revision.Decisiones.Single(d => d.IdTarea == tarea.IdTarea).Mover) tarea.IdUbicacion = destino;
        return null;
    }
}
