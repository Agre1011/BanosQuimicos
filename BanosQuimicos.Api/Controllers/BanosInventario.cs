using System.Globalization;
using System.Numerics;
using BanosQuimicos.Api.Contratos.Banos;
using BanosQuimicos.Api.Modelos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Controllers;

public partial class BanosController
{
    [HttpGet("tipos")]
    public async Task<IActionResult> Tipos(CancellationToken ct) => Ok(await db.TiposBano.AsNoTracking().OrderBy(t => t.IdTipoBano)
        .Select(t => new { t.IdTipoBano, Nombre = t.NombreTipo }).ToListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<EstadoBanoRespuesta>> Alta(AltaBanoSolicitud datos, CancellationToken ct)
    {
        if (datos.ClaveAlta == Guid.Empty) return BadRequest(new { mensaje = "Falta la clave del alta." });
        await using var tx = await Bloquear(ct);
        if (!await AdminActivo(ct)) return Forbid();
        var anterior = await db.BanosQuimicos.SingleOrDefaultAsync(b => b.ClaveAlta == datos.ClaveAlta, ct);
        if (anterior is not null)
        {
            if (anterior.IdTipoBano != datos.IdTipoBano) return Conflict(new { mensaje = "La clave corresponde a otro tipo de baño." });
            await tx.CommitAsync(ct); return Ok(await Estados(anterior.IdBano).SingleAsync(ct));
        }
        var tipo = await db.TiposBano.SingleOrDefaultAsync(t => t.IdTipoBano == datos.IdTipoBano, ct);
        if (tipo?.NombreTipo is not ("Obra" or "Evento")) return BadRequest(new { mensaje = "Elegir tipo Obra o Evento del catálogo." });
        var prefijo = tipo.NombreTipo == "Obra" ? "B.O-" : "B.E-";
        // Incluye bajas: el número histórico mayor nunca vuelve a asignarse. Los códigos de pruebas no son correlativos oficiales.
        var codigos = await db.BanosQuimicos.Where(b => b.CodigoInterno.StartsWith(prefijo)).Select(b => b.CodigoInterno).ToListAsync(ct);
        BigInteger mayor = 0;
        foreach (var codigo in codigos)
            if (BigInteger.TryParse(codigo.AsSpan(prefijo.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var numero) && numero > mayor) mayor = numero;
        var nuevoCodigo = prefijo + (mayor + 1).ToString("00", CultureInfo.InvariantCulture);
        if (nuevoCodigo.Length > 30) return Conflict(new { mensaje = "El correlativo supera la longitud técnica del código; requiere revisión del esquema." });
        var apto = await db.EstadosBano.SingleAsync(e => e.NombreEstado == "Apto", ct);
        var nuevo = new BanoQuimico { ClaveAlta = datos.ClaveAlta, IdTipoBano = tipo.IdTipoBano,
            CodigoInterno = nuevoCodigo, IdEstadoBano = apto.IdEstadoBano, FechaAlta = reloj.GetUtcNow() };
        db.BanosQuimicos.Add(nuevo); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return StatusCode(StatusCodes.Status201Created, await Estados(nuevo.IdBano).SingleAsync(ct));
    }

    [HttpPut("{id:int}/condicion")]
    public async Task<ActionResult<EstadoBanoRespuesta>> Condicion(int id, CondicionBanoSolicitud datos, CancellationToken ct)
    {
        await using var tx = await Bloquear(ct);
        if (!await AdminActivo(ct)) return Forbid();
        var b = await db.BanosQuimicos.Include(b => b.EstadoBano).SingleOrDefaultAsync(b => b.IdBano == id, ct);
        if (b is null) return NotFound();
        if (b.EstadoBano.NombreEstado != datos.Nueva)
        {
            if (b.EstadoBano.NombreEstado != datos.Anterior) return Conflict(new { mensaje = "La condición cambió; consultar nuevamente antes de confirmar." });
            b.IdEstadoBano = await db.EstadosBano.Where(e => e.NombreEstado == datos.Nueva).Select(e => e.IdEstadoBano).SingleAsync(ct);
            await db.SaveChangesAsync(ct);
        }
        // Cambiar condición no reactiva inventario ni cierra servicios ni mantenimientos.
        await tx.CommitAsync(ct); return Ok(await Estados(id).SingleAsync(ct));
    }

    [HttpPost("{id:int}/baja-inventario")]
    public async Task<ActionResult<EstadoBanoRespuesta>> BajaInventario(int id, CancellationToken ct)
    {
        await using var tx = await Bloquear(ct);
        if (!await AdminActivo(ct)) return Forbid();
        var b = await db.BanosQuimicos.SingleOrDefaultAsync(b => b.IdBano == id, ct);
        if (b is null) return NotFound();
        if (b.FechaBaja is null)
        {
            var servicios = await db.Servicios.Where(s => s.IdBano == id && (s.EstadoServicio == "Pendiente de entrega" || s.EstadoServicio == "Entregado"))
                .OrderBy(s => s.IdServicio).Select(s => new { s.IdServicio, s.EstadoServicio }).ToListAsync(ct);
            var tareas = await db.TareasOperativas.Where(t => t.IdBano == id && (t.IdEstadoOperacion == 6 || t.IdEstadoOperacion == 7))
                .OrderBy(t => t.IdTarea).Select(t => new { t.IdTarea, Tipo = t.TipoOperacion.NombreOperacion, Estado = t.EstadoOperacion.NombreEstado }).ToListAsync(ct);
            if (servicios.Count > 0 || tareas.Count > 0)
                return Conflict(new { mensaje = "No se puede dar de baja del inventario. " + string.Join("; ", servicios.Select(s => $"Servicio {s.IdServicio}: {s.EstadoServicio}")
                    .Concat(tareas.Select(t => $"Tarea {t.IdTarea}: {t.Tipo} ({t.Estado})"))) });
            if (reloj.GetUtcNow() < b.FechaAlta) return Conflict(new { mensaje = "La fecha actual es anterior al alta del baño." });
            b.FechaBaja = reloj.GetUtcNow(); await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct); return Ok(await Estados(id).SingleAsync(ct));
    }

    [HttpPost("{id:int}/mantenimientos")]
    public async Task<IActionResult> Mantenimiento(int id, MantenimientoBanoSolicitud datos, CancellationToken ct)
    {
        if (datos.ClaveProgramacion == Guid.Empty || datos.Fecha == default || string.IsNullOrWhiteSpace(datos.Descripcion))
            return BadRequest(new { mensaje = "Indicar descripción, empleado, fecha y clave válidos." });
        var descripcion = datos.Descripcion.Trim();
        var fecha = new DateTimeOffset(datos.Fecha.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-3));
        await using var tx = await Bloquear(ct);
        if (!await AdminActivo(ct)) return Forbid();
        var previa = await db.TareasOperativas.SingleOrDefaultAsync(t => t.ClaveProgramacion == datos.ClaveProgramacion, ct);
        if (previa is not null)
        {
            if (previa.IdBano != id || previa.IdServicio != null || previa.IdTipoOperacion != 11 || previa.IdEmpleado != datos.IdEmpleado
                || previa.FechaProgramada != fecha || previa.DescripcionTrabajo != descripcion || previa.BloqueaAsignacion != datos.BloqueaAsignacion)
                return Conflict(new { mensaje = "La clave pertenece a otra programación." });
            await tx.CommitAsync(ct); return Ok(new { previa.IdTarea });
        }
        var b = await db.BanosQuimicos.SingleOrDefaultAsync(b => b.IdBano == id, ct);
        if (b is null) return NotFound();
        if (b.FechaBaja is not null) return Conflict(new { mensaje = "El baño está dado de baja del inventario y no admite trabajos nuevos." });
        var servicio = await db.Servicios.Where(s => s.IdBano == id && (s.EstadoServicio == "Pendiente de entrega" || s.EstadoServicio == "Entregado"))
            .Select(s => (int?)s.IdServicio).FirstOrDefaultAsync(ct);
        if (servicio is not null) return Conflict(new { mensaje = $"El baño tiene servicio vigente {servicio}; esta operación programa mantenimiento sin servicio." });
        if (!await EmpleadoActivo(datos.IdEmpleado, ct)) return Conflict(new { mensaje = "Elegir un empleado activo." });
        var tarea = new TareaOperativa { IdBano = id, IdEmpleado = datos.IdEmpleado, IdTipoOperacion = 11,
            IdEstadoOperacion = 6, FechaRegistro = reloj.GetUtcNow(), FechaProgramada = fecha,
            DescripcionTrabajo = descripcion, BloqueaAsignacion = datos.BloqueaAsignacion, ClaveProgramacion = datos.ClaveProgramacion };
        db.TareasOperativas.Add(tarea); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return StatusCode(StatusCodes.Status201Created, new { tarea.IdTarea });
    }
}
