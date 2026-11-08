using System.ComponentModel.DataAnnotations;

namespace BanosQuimicos.App.Autenticacion;

public class DatosDireccionCliente : IValidatableObject
{
    public Guid ClaveEnvio { get; set; } = Guid.NewGuid();
    public int IdUbicacionEsperada { get; set; }
    [Required(ErrorMessage = "Ingresá la nueva dirección."), StringLength(300)] public string NuevaDireccion { get; set; } = "";
    [Required(ErrorMessage = "Elegí una modalidad.")] public string Modalidad { get; set; } = "";
    public DateTime? FechaLocal { get; set; }
    public bool Intentado { get; set; }
    public DateTimeOffset? FechaEfectiva => Modalidad == "Informado" && FechaLocal is { } fecha
        ? new DateTimeOffset(DateTime.SpecifyKind(fecha, DateTimeKind.Unspecified), TimeSpan.FromHours(-3)) : null;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(NuevaDireccion)) yield return new("Ingresá la nueva dirección.", [nameof(NuevaDireccion)]);
        if (Modalidad == "Informado" && (FechaEfectiva is null || FechaEfectiva > DateTimeOffset.UtcNow))
            yield return new("Indicá fecha y hora efectivas no futuras (Argentina UTC-3).", [nameof(FechaLocal)]);
    }
}
public class DatosBajaCliente { public Guid ClaveEnvio { get; set; } = Guid.NewGuid(); public bool Intentado { get; set; } }
public record DecisionUbicacion(int IdTarea, int? IdUbicacion, bool Mover);
public record RevisarDireccion(Guid ClaveRevision, int IdUbicacionEsperada, int IdEstadoAnterior, List<DecisionUbicacion> Decisiones, string? Explicacion);
public record TareaRevision(int IdTarea, string Tipo, string Estado, int? IdUbicacion, string? Direccion, bool ImpideContinuar);
public record RevisionDireccion(int IdSolicitud, int IdEstado, string Estado, string Modalidad,
    int IdOrigen, string DireccionActual, string NuevaDireccion, DateTimeOffset? FechaEfectiva, bool Revisada, List<TareaRevision> Tareas);
