using System.Security.Claims;
using BanosQuimicos.Api.Contratos.Cuentas;
using BanosQuimicos.Api.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Controllers;

public partial class CuentasController
{
    // El mismo bloqueo que programación/asignación/cierre evita deshabilitar a alguien
    // entre la comprobación de sus trabajos y una asignación nueva.
    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> Bloquear(CancellationToken ct)
    {
        var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        try { await BanosQuimicos.Api.Reglas.ReglasDireccion.Bloquear(db, ct); return tx; }
        catch { await tx.DisposeAsync(); throw; }
    }
    private async Task<bool> AdministradorActual(CancellationToken ct) => await db.Usuarios.AnyAsync(x =>
        x.IdUsuario == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) && x.EstadoCuenta == "Activa" && x.Rol.NombreRol == "Administrador", ct);

    private async Task<List<string>> Bloqueos(int id, bool trabajos, bool servicios, CancellationToken ct)
    {
        var elementos = new List<string>();
        if (trabajos)
            elementos.AddRange(await db.TareasOperativas.Where(t => t.IdEmpleado == id && (t.IdEstadoOperacion == 6 || t.IdEstadoOperacion == 7
                    || t.FechaTrabajoRegistrado != null && t.FechaRealizada == null && t.IdEstadoOperacion != 8))
                .OrderBy(t => t.IdTarea).Select(t => "Tarea " + t.IdTarea + ": " + t.TipoOperacion.NombreOperacion + " (" + t.EstadoOperacion.NombreEstado + ")").ToListAsync(ct));
        if (servicios)
        {
            elementos.AddRange(await db.Servicios.Where(s => s.Cliente.IdUsuario == id && (s.EstadoServicio == "Pendiente de entrega" || s.EstadoServicio == "Entregado"))
                .OrderBy(s => s.IdServicio).Select(s => "Servicio " + s.IdServicio + ": " + s.EstadoServicio).ToListAsync(ct));
            elementos.AddRange(await db.SolicitudesCliente.Where(s => s.Cliente.IdUsuario == id && (s.IdEstadoOperacion == 1 || s.IdEstadoOperacion == 2))
                .OrderBy(s => s.IdSolicitud).Select(s => "Solicitud " + s.IdSolicitud + ": " + s.TipoOperacion.NombreOperacion + " (" + s.EstadoOperacion.NombreEstado + ")").ToListAsync(ct));
        }
        return elementos;
    }

    [HttpPut("{id:int}/rol")]
    public async Task<IActionResult> CambiarRol(int id, CambioRolSolicitud datos, CancellationToken ct)
    {
        if (datos.RolAnterior == datos.NuevoRol) return BadRequest(new { mensaje = "Elegir un rol distinto del actual." });
        await using var tx = await Bloquear(ct);
        if (!await AdministradorActual(ct)) return Forbid();
        var u = await db.Usuarios.Include(x => x.Rol).SingleOrDefaultAsync(x => x.IdUsuario == id, ct);
        if (u is null) return NotFound();
        if (u.Rol.NombreRol != datos.RolAnterior) return Conflict(new { mensaje = "El rol cambió o la cuenta no es Cliente/Empleado. Consultá su estado actual." });
        var bloqueos = await Bloqueos(id, datos.RolAnterior == "Empleado", datos.RolAnterior == "Cliente", ct);
        if (bloqueos.Count > 0) return Conflict(new { mensaje = "No se puede cambiar el rol. Resolver antes: " + string.Join("; ", bloqueos) });
        u.IdRol = await db.Roles.Where(r => r.NombreRol == datos.NuevoRol).Select(r => r.IdRol).SingleAsync(ct);
        // No tocar estado, contraseña, perfil ni referencias históricas/habituales.
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return NoContent();
    }

    [HttpPost("{id:int}/activar")]
    public async Task<IActionResult> Activar(int id, CancellationToken ct)
    {
        await using var tx = await Bloquear(ct);
        if (!await AdministradorActual(ct)) return Forbid();
        var u = await db.Usuarios.Include(x => x.Rol).Include(x => x.Cliente).SingleOrDefaultAsync(x => x.IdUsuario == id, ct);
        if (u is null) return NotFound();
        if (u.EstadoCuenta != "Pendiente" || u.Rol.NombreRol is not ("Cliente" or "Empleado") || u.Cliente is null)
            return Conflict(new { mensaje = "Solo se activan cuentas antiguas Pendientes Cliente/Empleado con perfil de cliente." });
        u.EstadoCuenta = "Activa";
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return NoContent();
    }

    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, CambioEstadoSolicitud datos, CancellationToken ct)
    {
        var suspender = datos.EstadoAnterior == "Activa" && datos.NuevoEstado == "Suspendida";
        var reactivar = datos.EstadoAnterior == "Suspendida" && datos.NuevoEstado == "Activa";
        if (!suspender && !reactivar && datos.NuevoEstado != "BajaLogica")
            return BadRequest(new { mensaje = "Transición no permitida. Para una cuenta antigua Pendiente usar Activar cuenta." });
        if (suspender && string.IsNullOrWhiteSpace(datos.Motivo)) return BadRequest(new { mensaje = "Indicá el motivo de suspensión." });
        if (!suspender && datos.Motivo is not null) return BadRequest(new { mensaje = "El motivo corresponde únicamente a suspensión." });
        await using var tx = await Bloquear(ct);
        if (!await AdministradorActual(ct)) return Forbid();
        var u = await db.Usuarios.Include(x => x.Rol).SingleOrDefaultAsync(x => x.IdUsuario == id, ct);
        if (u is null) return NotFound();
        if (u.IdUsuario == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!))
            return Conflict(new { mensaje = "No podés cambiar tu propio estado administrativo." });
        // Repetir el mismo resultado no reemplaza fechas ni motivos.
        if (u.EstadoCuenta == datos.NuevoEstado) { await tx.CommitAsync(ct); return NoContent(); }
        if (u.EstadoCuenta == "BajaLogica") return Conflict(new { mensaje = "La baja lógica es definitiva desde la app. No se puede reactivar." });
        if (u.EstadoCuenta != datos.EstadoAnterior) return Conflict(new { mensaje = "El estado cambió. Consultá la cuenta antes de confirmar." });
        if (!reactivar)
        {
            if (u.Rol.NombreRol == "Administrador" && await db.Usuarios.CountAsync(x => x.EstadoCuenta == "Activa" && x.Rol.NombreRol == "Administrador", ct) <= 1)
                return Conflict(new { mensaje = "Debe conservarse al menos un administrador activo." });
            var bloqueos = await Bloqueos(id, true, true, ct);
            if (bloqueos.Count > 0) return Conflict(new { mensaje = "No se puede deshabilitar la cuenta. Resolver antes: " + string.Join("; ", bloqueos) });
        }
        u.EstadoCuenta = datos.NuevoEstado;
        if (suspender) u.MotivoSuspension = datos.Motivo!.Trim();
        if (datos.NuevoEstado == "BajaLogica") u.FechaBaja = DateTimeOffset.UtcNow;
        // Reactivar conserva rol, historial y último motivo; no elimina perfiles ni trabajos.
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return NoContent();
    }
}
