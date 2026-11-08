using System.ComponentModel.DataAnnotations;

namespace BanosQuimicos.App.Autenticacion;

public class DatosNuevoServicio
{
    public Guid ClaveEnvio { get; set; } = Guid.NewGuid();
    private string direccion = "";
    [Required(ErrorMessage = "Ingresá la dirección."), StringLength(300)]
    public string Direccion { get => direccion; set => direccion = value?.Trim() ?? ""; }
    [Range(1, int.MaxValue, ErrorMessage = "Indicá una cantidad mayor a cero.")]
    public int CantidadBanos { get; set; } = 1;
    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná el tipo de baño.")]
    public int IdTipoBano { get; set; }
    [Required(ErrorMessage = "Seleccioná el uso del servicio."), RegularExpression("^(Obra|Evento)$")]
    public string TipoUso { get; set; } = "";
    [Required(ErrorMessage = "Seleccioná la fecha deseada.")]
    public DateOnly? FechaDeseada { get; set; }
    // No se serializa: bloquea el contenido del envío mientras su resultado es incierto.
    [System.Text.Json.Serialization.JsonIgnore] public bool Intentado { get; set; }
}

public record TipoBanoPedido(int IdTipoBano, string Nombre);
public record SolicitudPropia(int IdSolicitud, string Tipo, string Estado, DateTimeOffset FechaSolicitud,
    string? Descripcion, int? CantidadBanos, string? TipoBano, string? TipoUso, string? Direccion,
    DateTimeOffset? FechaDeseada, List<string> Banos, string? Modalidad = null, string? NuevaDireccion = null, DateTimeOffset? FechaEfectiva = null, string? RespuestaAdministrativa = null, DateTimeOffset? FechaResolucion = null);
