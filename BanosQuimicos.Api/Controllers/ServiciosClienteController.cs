using System.Security.Claims;
using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Controllers;

public record ServicioClienteRespuesta(int IdServicio, string CodigoBano, string TipoBano, string TipoUso,
    string? Direccion, int CantidadLimpiezasSemana, string EstadoServicio, int? IdUbicacionActual = null);

[ApiController]
[Route("api/servicios-cliente")]
[Authorize(Policy = Politicas.Cliente)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ServiciosClienteController(BanosQuimicosDbContext db) : ControllerBase
{
    // Vigente significa que conserva la asignación, incluso antes de la entrega o al pedir la baja.
    // La identidad procede de la sesión, nunca de un parámetro elegido por el cliente.
    private IQueryable<ServicioClienteRespuesta> Propios(int? id = null)
    {
        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return db.Servicios.AsNoTracking()
            .Where(s => s.Cliente.IdUsuario == idUsuario &&
                (id == null || s.IdServicio == id) &&
                (s.EstadoServicio == "Pendiente de entrega" || s.EstadoServicio == "Entregado"))
            .OrderBy(s => s.Bano.CodigoInterno).ThenBy(s => s.IdServicio)
            .Select(s => new ServicioClienteRespuesta(s.IdServicio, s.Bano.CodigoInterno,
                s.Bano.TipoBano.NombreTipo, s.TipoUso,
                // Solo ubicaciones del servicio propio: actual o preparada antes de entregar.
                db.UbicacionesServicio.Where(u => u.IdServicio == s.IdServicio)
                    .OrderByDescending(u => u.EsActual).ThenByDescending(u => u.IdUbicacion)
                    .Select(u => u.Direccion).FirstOrDefault(),
                s.CantidadLimpiezasSemana, s.EstadoServicio,
                db.UbicacionesServicio.Where(u => u.IdServicio == s.IdServicio && u.EsActual).Select(u => (int?)u.IdUbicacion).FirstOrDefault()));
    }

    [HttpGet("mios/resumen")]
    public async Task<IActionResult> Resumen(CancellationToken ct) =>
        Ok(new { cantidad = await Propios().CountAsync(ct) });

    [HttpGet("mios")]
    public async Task<ActionResult<List<ServicioClienteRespuesta>>> Listar(CancellationToken ct) =>
        Ok(await Propios().ToListAsync(ct));

    [HttpGet("mios/pagina")]
    public async Task<IActionResult> Pagina(CancellationToken ct, [FromQuery, System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] int pagina = 1) =>
        Ok(await BanosQuimicos.Api.Contratos.ConsultaPaginada.Leer(Propios(), pagina, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServicioClienteRespuesta>> Detalle(int id, CancellationToken ct)
    {
        var servicio = await Propios(id).SingleOrDefaultAsync(ct);
        return servicio is null ? NotFound() : Ok(servicio);
    }
}
