using System.Data;
using System.Security.Claims;
using BanosQuimicos.Api.Contratos.Solicitudes;
using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Modelos;
using BanosQuimicos.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Controllers;

[ApiController]
[Route("api/solicitudes-cliente")]
[Authorize(Policy = Politicas.Cliente)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public partial class SolicitudesClienteController(BanosQuimicosDbContext db, TimeProvider reloj) : ControllerBase
{
    private int Usuario => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IQueryable<SolicitudPropiaRespuesta> Propias(int? id = null) => db.SolicitudesCliente.AsNoTracking()
        .Where(s => s.Cliente.IdUsuario == Usuario && (id == null || s.IdSolicitud == id))
        .OrderByDescending(s => s.FechaSolicitud).ThenByDescending(s => s.IdSolicitud)
        .Select(s => new SolicitudPropiaRespuesta(s.IdSolicitud, s.TipoOperacion.NombreOperacion,
            s.EstadoOperacion.NombreEstado, s.FechaSolicitud, s.Descripcion, s.CantidadBanos,
            s.TipoBanoSolicitado == null ? null : s.TipoBanoSolicitado.NombreTipo,
            s.TipoUsoSolicitado, s.DireccionSolicitada, s.FechaDeseada,
            db.Servicios.Where(v => v.IdSolicitudOrigen == s.IdSolicitud || v.IdServicio == s.IdServicioReferencia)
                .OrderBy(v => v.IdServicio).Select(v => v.Bano.CodigoInterno).ToList(), s.ModalidadCambioDireccion, s.NuevaDireccion, s.FechaCambioEfectiva,
            s.IdTipoOperacion == 5 && s.IdEstadoOperacion == 4 ? s.ObservacionesAdministrador : null, s.FechaResolucion));

    [HttpGet("mias")]
    public async Task<ActionResult<List<SolicitudPropiaRespuesta>>> Listar(CancellationToken ct) =>
        Ok(await Propias().ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SolicitudPropiaRespuesta>> Detalle(int id, CancellationToken ct)
    {
        var s = await Propias(id).SingleOrDefaultAsync(ct);
        return s is null ? NotFound() : Ok(s);
    }

    [HttpGet("tipos-bano")]
    public async Task<IActionResult> Tipos(CancellationToken ct) => Ok(await db.TiposBano.AsNoTracking()
        .OrderBy(t => t.IdTipoBano).Select(t => new { idTipoBano = t.IdTipoBano, nombre = t.NombreTipo }).ToListAsync(ct));

    [HttpPost("nuevo-servicio")]
    public async Task<ActionResult<SolicitudPropiaRespuesta>> Crear(NuevoServicioSolicitud datos, CancellationToken ct)
    {
        var direccion = datos.Direccion.Trim();
        if (datos.ClaveEnvio == Guid.Empty || direccion.Length == 0 || datos.FechaDeseada == default)
            return BadRequest(new { mensaje = "Indicar clave, dirección y fecha válidas." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Ordenar reintentos concurrentes, incluso si el primero guardó pero se perdió su respuesta.
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @r int;
            EXEC @r=sys.sp_getapplock @Resource=N'BanosQuimicos.CumplimientoTareas',
                @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
            IF @r<0 THROW 51000, 'No se pudo bloquear el envío.', 1;
            """, ct);
        var cliente = await db.Clientes.Where(c => c.IdUsuario == Usuario && c.Usuario.EstadoCuenta == "Activa"
            && c.Usuario.Rol.NombreRol == "Cliente").SingleOrDefaultAsync(ct);
        if (cliente is null) return Forbid();
        var anterior = await db.SolicitudesCliente.SingleOrDefaultAsync(s => s.ClaveEnvio == datos.ClaveEnvio, ct);
        var fecha = new DateTimeOffset(datos.FechaDeseada.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-3));
        if (anterior is not null)
        {
            // Nunca revelar una solicitud ajena aunque alguien conozca su clave.
            if (anterior.IdCliente != cliente.IdCliente || anterior.IdTipoOperacion != 1
                || anterior.DireccionSolicitada != direccion || anterior.CantidadBanos != datos.CantidadBanos
                || anterior.IdTipoBanoSolicitado != datos.IdTipoBano || anterior.TipoUsoSolicitado != datos.TipoUso
                || anterior.FechaDeseada != fecha)
                return Conflict(new { mensaje = "La clave no corresponde a este envío." });
            await tx.CommitAsync(ct);
            return Ok(await Propias(anterior.IdSolicitud).SingleAsync(ct));
        }
        if (!await db.TiposBano.AnyAsync(t => t.IdTipoBano == datos.IdTipoBano, ct))
            return BadRequest(new { mensaje = "Elegir un tipo de baño existente." });
        // La fecha deseada es una referencia, no una reserva ni una fecha de entrega confirmada.
        var nueva = new SolicitudCliente { ClaveEnvio = datos.ClaveEnvio, IdCliente = cliente.IdCliente,
            IdTipoOperacion = 1, IdEstadoOperacion = 1, FechaSolicitud = reloj.GetUtcNow(),
            DireccionSolicitada = direccion, CantidadBanos = datos.CantidadBanos, IdTipoBanoSolicitado = datos.IdTipoBano,
            TipoUsoSolicitado = datos.TipoUso, FechaDeseada = fecha };
        db.SolicitudesCliente.Add(nueva); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return CreatedAtAction(nameof(Detalle), new { id = nueva.IdSolicitud }, await Propias(nueva.IdSolicitud).SingleAsync(ct));
    }

    [HttpPost("servicios/{id:int}/limpieza-extra")]
    public Task<ActionResult<SolicitudPropiaRespuesta>> Limpieza(int id, SolicitudServicioSolicitud datos, CancellationToken ct) =>
        CrearSobreServicio(id, 3, datos, ct);

    [HttpPost("servicios/{id:int}/reclamo")]
    public Task<ActionResult<SolicitudPropiaRespuesta>> Reclamo(int id, SolicitudServicioSolicitud datos, CancellationToken ct) =>
        CrearSobreServicio(id, 5, datos, ct);

    private async Task<ActionResult<SolicitudPropiaRespuesta>> CrearSobreServicio(int id, int tipo, SolicitudServicioSolicitud datos, CancellationToken ct)
    {
        var descripcion = datos.Descripcion?.Trim();
        if (datos.ClaveEnvio == Guid.Empty || tipo == 5 && string.IsNullOrWhiteSpace(descripcion)
            || tipo == 3 && !string.IsNullOrEmpty(descripcion))
            return BadRequest(new { mensaje = "Indicar una clave válida y descripción obligatoria solo para reclamos." });
        if (tipo == 3) descripcion = null; // No inventar observaciones para una limpieza confirmada.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await db.Database.ExecuteSqlRawAsync("""
            DECLARE @r int;
            EXEC @r=sys.sp_getapplock @Resource=N'BanosQuimicos.CumplimientoTareas',
                @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
            IF @r<0 THROW 51000, 'No se pudo bloquear el envío.', 1;
            """, ct);
        var cliente = await db.Clientes.Where(c => c.IdUsuario == Usuario && c.Usuario.EstadoCuenta == "Activa"
            && c.Usuario.Rol.NombreRol == "Cliente").SingleOrDefaultAsync(ct);
        if (cliente is null) return Forbid();
        var anterior = await db.SolicitudesCliente.SingleOrDefaultAsync(s => s.ClaveEnvio == datos.ClaveEnvio, ct);
        if (anterior is not null)
        {
            if (anterior.IdCliente != cliente.IdCliente || anterior.IdServicioReferencia != id
                || anterior.IdTipoOperacion != tipo || anterior.Descripcion != descripcion)
                return Conflict(new { mensaje = "La clave no corresponde a este envío." });
            // Reintentar un guardado no es pedir otra operación: conservarlo aunque el servicio haya finalizado.
            await tx.CommitAsync(ct); return Ok(await Propias(anterior.IdSolicitud).SingleAsync(ct));
        }
        var servicio = await db.Servicios.SingleOrDefaultAsync(s => s.IdServicio == id && s.IdCliente == cliente.IdCliente, ct);
        if (servicio is null) return NotFound();
        if (servicio.EstadoServicio != "Entregado")
            return Conflict(new { mensaje = "La solicitud requiere un servicio entregado." });
        var nueva = new SolicitudCliente { ClaveEnvio = datos.ClaveEnvio, IdCliente = cliente.IdCliente,
            IdServicioReferencia = id, IdTipoOperacion = tipo, IdEstadoOperacion = 1,
            FechaSolicitud = reloj.GetUtcNow(), Descripcion = descripcion };
        db.SolicitudesCliente.Add(nueva); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return CreatedAtAction(nameof(Detalle), new { id = nueva.IdSolicitud }, await Propias(nueva.IdSolicitud).SingleAsync(ct));
    }
}
