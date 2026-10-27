namespace BanosQuimicos.Api.Modelos;

public class SolicitudCliente
{
    public int IdSolicitud { get; set; }
    public int IdCliente { get; set; }
    public int IdTipoOperacion { get; set; }
    public int IdEstadoOperacion { get; set; }
    public DateTimeOffset FechaSolicitud { get; set; }
    public int? IdServicioReferencia { get; set; }
    public string? Descripcion { get; set; }
    public int? CantidadBanos { get; set; }
    public int? IdTipoBanoSolicitado { get; set; }
    public string? TipoUsoSolicitado { get; set; }
    public string? DireccionSolicitada { get; set; }
    public DateTimeOffset? FechaDeseada { get; set; }
    public string? NuevaDireccion { get; set; }
    public string? ModalidadCambioDireccion { get; set; }
    public DateTimeOffset? FechaCambioEfectiva { get; set; }
    public DateTimeOffset? FechaValidacion { get; set; }
    public DateTimeOffset? FechaResolucion { get; set; }
    public string? ObservacionesAdministrador { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public TipoOperacion TipoOperacion { get; set; } = null!;
    public EstadoOperacion EstadoOperacion { get; set; } = null!;
    public Servicio? ServicioReferencia { get; set; }
    public TipoBano? TipoBanoSolicitado { get; set; }
}
