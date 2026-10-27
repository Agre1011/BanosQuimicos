namespace BanosQuimicos.Api.Modelos;

public class Servicio
{
    public int IdServicio { get; set; }
    public int IdCliente { get; set; }
    public int IdBano { get; set; }
    public int? IdSolicitudOrigen { get; set; }
    public string TipoUso { get; set; } = string.Empty;
    public int CantidadLimpiezasSemana { get; set; }
    public string EstadoServicio { get; set; } = string.Empty;
    public DateTimeOffset FechaAsignacion { get; set; }
    public DateTimeOffset? FechaEntregaProgramada { get; set; }
    public DateTimeOffset? FechaEntregaReal { get; set; }
    public DateTimeOffset? FechaRetiroReal { get; set; }
    public DateTimeOffset? FechaFinalizacion { get; set; }
    public DateTimeOffset? FechaCancelacion { get; set; }
    public string? Observaciones { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public BanoQuimico Bano { get; set; } = null!;
    public SolicitudCliente? SolicitudOrigen { get; set; }
}
