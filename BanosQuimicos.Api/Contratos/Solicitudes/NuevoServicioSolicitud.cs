using System.ComponentModel.DataAnnotations;

namespace BanosQuimicos.Api.Contratos.Solicitudes;

public class NuevoServicioSolicitud
{
    public Guid ClaveEnvio { get; set; }
    [Required, StringLength(300)] public string Direccion { get; set; } = "";
    [Range(1, int.MaxValue)] public int CantidadBanos { get; set; }
    [Range(1, int.MaxValue)] public int IdTipoBano { get; set; }
    [Required, RegularExpression("^(Obra|Evento)$")] public string TipoUso { get; set; } = "";
    public DateOnly FechaDeseada { get; set; }
}

public record SolicitudPropiaRespuesta(int IdSolicitud, string Tipo, string Estado, DateTimeOffset FechaSolicitud,
    string? Descripcion, int? CantidadBanos, string? TipoBano, string? TipoUso, string? Direccion,
    DateTimeOffset? FechaDeseada, List<string> Banos, string? Modalidad = null, string? NuevaDireccion = null, DateTimeOffset? FechaEfectiva = null, string? RespuestaAdministrativa = null, DateTimeOffset? FechaResolucion = null);
