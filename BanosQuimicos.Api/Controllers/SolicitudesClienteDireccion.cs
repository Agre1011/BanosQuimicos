using System.Data;
using BanosQuimicos.Api.Contratos.Solicitudes;
using BanosQuimicos.Api.Modelos;
using BanosQuimicos.Api.Reglas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Controllers;

public partial class SolicitudesClienteController
{
    [HttpPost("servicios/{id:int}/cambio-direccion")]
    public Task<ActionResult<SolicitudPropiaRespuesta>> Cambio(int id, CambioDireccionSolicitud datos, CancellationToken ct) => CrearDireccionBaja(id, datos, null, ct);
    [HttpPost("servicios/{id:int}/baja")]
    public Task<ActionResult<SolicitudPropiaRespuesta>> Baja(int id, BajaClienteSolicitud datos, CancellationToken ct) => CrearDireccionBaja(id, null, datos, ct);

    private async Task<ActionResult<SolicitudPropiaRespuesta>> CrearDireccionBaja(int id, CambioDireccionSolicitud? cambio, BajaClienteSolicitud? baja, CancellationToken ct)
    {
        var clave = cambio?.ClaveEnvio ?? baja!.ClaveEnvio; var tipo = cambio is null ? 6 : 4;
        var direccion = cambio?.NuevaDireccion.Trim(); var ahora = reloj.GetUtcNow();
        if (clave == Guid.Empty || cambio is not null && (string.IsNullOrWhiteSpace(direccion)
            || cambio.Modalidad == "Informado" && (cambio.FechaEfectiva is null || cambio.FechaEfectiva > ahora)
            || cambio.Modalidad == "Solicitado" && cambio.FechaEfectiva is not null))
            return BadRequest(new { mensaje = "Revisá dirección, modalidad y fecha/hora efectivas no futuras para el aviso informado." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await ReglasDireccion.Bloquear(db, ct);
        var cliente = await db.Clientes.SingleOrDefaultAsync(c => c.IdUsuario == Usuario && c.Usuario.EstadoCuenta == "Activa" && c.Usuario.Rol.NombreRol == "Cliente", ct);
        if (cliente is null) return Forbid();
        var previa = await db.SolicitudesCliente.SingleOrDefaultAsync(s => s.ClaveEnvio == clave, ct);
        if (previa is not null)
        {
            if (previa.IdCliente != cliente.IdCliente || previa.IdServicioReferencia != id || previa.IdTipoOperacion != tipo
                || previa.NuevaDireccion != direccion || previa.ModalidadCambioDireccion != cambio?.Modalidad
                || cambio?.Modalidad == "Informado" && previa.FechaCambioEfectiva != cambio.FechaEfectiva
                || cambio is not null && ReglasDireccion.Leer(previa)?.Origen != cambio.IdUbicacionEsperada)
                return Conflict(new { mensaje = "La clave no corresponde a este envío." });
            await tx.CommitAsync(ct); return Ok(await Propias(previa.IdSolicitud).SingleAsync(ct));
        }
        var servicio = await db.Servicios.SingleOrDefaultAsync(s => s.IdServicio == id && s.IdCliente == cliente.IdCliente, ct);
        if (servicio is null) return NotFound();
        if (servicio.EstadoServicio != "Entregado") return Conflict(new { mensaje = "La operación requiere un servicio Entregado." });
        var conflicto = await ReglasDireccion.Conflicto(db, id, null, ct);
        if (conflicto is not null) return Conflict(new { mensaje = "Ya existe una operación abierta incompatible: " + conflicto });
        var origen = await db.UbicacionesServicio.SingleOrDefaultAsync(u => u.IdServicio == id && u.EsActual, ct);
        if (origen is null || cambio is not null && origen.IdUbicacion != cambio.IdUbicacionEsperada)
            return Conflict(new { mensaje = "La ubicación actual cambió. Consultá nuevamente el servicio." });
        if (cambio is not null && (string.Equals(origen.Direccion.Trim(), direccion, StringComparison.OrdinalIgnoreCase)
            || cambio.FechaEfectiva < origen.FechaDesde))
            return BadRequest(new { mensaje = "Indicá una dirección distinta y una fecha efectiva no anterior al inicio de la ubicación actual." });
        var nueva = new SolicitudCliente { ClaveEnvio = clave, IdCliente = cliente.IdCliente, IdServicioReferencia = id,
            IdTipoOperacion = tipo, IdEstadoOperacion = 1, FechaSolicitud = ahora, NuevaDireccion = direccion,
            ModalidadCambioDireccion = cambio?.Modalidad, FechaCambioEfectiva = cambio?.FechaEfectiva };
        if (cambio is not null) ReglasDireccion.Guardar(nueva, new(origen.IdUbicacion, 0, Guid.Empty, null, null, null));
        db.SolicitudesCliente.Add(nueva); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return CreatedAtAction(nameof(Detalle), new { id = nueva.IdSolicitud }, await Propias(nueva.IdSolicitud).SingleAsync(ct));
    }
}
