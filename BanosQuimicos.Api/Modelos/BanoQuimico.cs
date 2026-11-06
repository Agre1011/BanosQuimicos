namespace BanosQuimicos.Api.Modelos;

public class BanoQuimico
{
    public int IdBano { get; set; }
    public string CodigoInterno { get; set; } = string.Empty;
    public int IdTipoBano { get; set; }
    public int IdEstadoBano { get; set; }
    public DateTimeOffset FechaAlta { get; set; }
    public string? Observaciones { get; set; }
    public int? IdResponsableLimpieza { get; set; }
    public Usuario? ResponsableLimpieza { get; set; }
    public TipoBano TipoBano { get; set; } = null!;
    public EstadoBano EstadoBano { get; set; } = null!;
}
