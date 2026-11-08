using System.ComponentModel.DataAnnotations;

namespace BanosQuimicos.Api.Contratos.Solicitudes;

public class CambioDireccionSolicitud
{
    public Guid ClaveEnvio { get; set; }
    [Required, StringLength(300)] public string NuevaDireccion { get; set; } = "";
    [Required, RegularExpression("^(Solicitado|Informado)$")] public string Modalidad { get; set; } = "";
    [Range(1, int.MaxValue)] public int IdUbicacionEsperada { get; set; }
    public DateTimeOffset? FechaEfectiva { get; set; }
}
public class BajaClienteSolicitud { public Guid ClaveEnvio { get; set; } }
public record DecisionUbicacionSolicitud(int IdTarea, int? IdUbicacion, bool Mover);
public class RevisarDireccionSolicitud
{
    public Guid ClaveRevision { get; set; }
    public int IdUbicacionEsperada { get; set; }
    public int IdEstadoAnterior { get; set; }
    [Required] public List<DecisionUbicacionSolicitud> Decisiones { get; set; } = [];
    [StringLength(2000)] public string? Explicacion { get; set; }
}
public record TareaRevisionRespuesta(int IdTarea, string Tipo, string Estado, int? IdUbicacion,
    string? Direccion, bool ImpideContinuar);
public record RevisionDireccionRespuesta(int IdSolicitud, int IdEstado, string Estado, string Modalidad,
    int IdOrigen, string DireccionActual, string NuevaDireccion, DateTimeOffset? FechaEfectiva,
    bool Revisada, List<TareaRevisionRespuesta> Tareas);
