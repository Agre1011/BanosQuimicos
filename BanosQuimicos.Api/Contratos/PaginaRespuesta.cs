using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Contratos;

public record PaginaRespuesta<T>(List<T> Elementos, int Pagina, int TamanoPagina, int Total);

public static class ConsultaPaginada
{
    // El llamador aplica propietario, filtros y orden estable antes de paginar en SQL.
    public static async Task<PaginaRespuesta<T>> Leer<T>(IQueryable<T> consulta, int pagina, CancellationToken ct)
    {
        const int tamano = 10;
        var total = await consulta.CountAsync(ct);
        var ultima = Math.Max(1, (int)(((long)total + tamano - 1) / tamano));
        pagina = Math.Clamp(pagina, 1, ultima);
        var elementos = await consulta.Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(ct);
        return new(elementos, pagina, tamano, total);
    }
}
