using System.ComponentModel.DataAnnotations;

namespace BanosQuimicos.Api.Contratos.Autenticacion;

public class LoginSolicitud
{
    private string email = "";
    [Required, EmailAddress, StringLength(254)]
    public string Email { get => email; set => email = value?.Trim() ?? ""; }
    [Required, StringLength(128)]
    public string Contrasena { get; set; } = "";
}
