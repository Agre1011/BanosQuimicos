using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BanosQuimicos.Api.Contratos.Autenticacion;

// Rechaza campos extra como IdRol o EstadoCuenta en lugar de permitir sobreasignación.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegistroClienteSolicitud
{
    private string nombre = "", apellido = "", email = "", telefono = "";
    [Required, StringLength(100)]
    public string Nombre { get => nombre; set => nombre = value?.Trim() ?? ""; }
    [Required, StringLength(100)]
    public string Apellido { get => apellido; set => apellido = value?.Trim() ?? ""; }
    [Required, EmailAddress, StringLength(254)]
    public string Email { get => email; set => email = value?.Trim() ?? ""; }
    [Required, StringLength(40)]
    public string Telefono { get => telefono; set => telefono = value?.Trim() ?? ""; }
    [Required, StringLength(128, MinimumLength = 12)]
    public string Contrasena { get; set; } = "";
    [StringLength(200)] public string? RazonSocial { get; set; }
    [StringLength(20)] public string? Cuit { get; set; }
    [StringLength(300)] public string? DireccionPrincipal { get; set; }
}
