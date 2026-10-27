namespace BanosQuimicos.Api.Modelos;

public class UbicacionServicio
{
    public int IdUbicacion { get; set; }
    public int IdServicio { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public string? Localidad { get; set; }
    public string? Referencia { get; set; }
    public DateTimeOffset FechaRegistro { get; set; }
    public bool EsActual { get; set; }
    public DateTimeOffset? FechaDesde { get; set; }
    public DateTimeOffset? FechaHasta { get; set; }
    public DateTimeOffset? FechaValidacion { get; set; }
    public Servicio Servicio { get; set; } = null!;
}
