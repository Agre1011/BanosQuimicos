using System.Security.Claims;
using BanosQuimicos.Api.Contratos.Tareas;
using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Modelos;
using BanosQuimicos.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            x.Servicio == null ? null : x.Servicio.Cliente.Usuario.Nombre + " " + x.Servicio.Cliente.Usuario.Apellido));

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
}
