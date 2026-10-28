using System.ComponentModel.DataAnnotations;

namespace BanosQuimicos.App.Autenticacion;

public class DatosLogin
{
    private string email = "";
    [Required(ErrorMessage = "Ingresá tu correo electrónico."), EmailAddress(ErrorMessage = "Ingresá un correo válido."), StringLength(254)]
    public string Email { get => email; set => email = value?.Trim() ?? ""; }
    [Required(ErrorMessage = "Ingresá tu contraseña."), StringLength(128, ErrorMessage = "La contraseña no puede superar los 128 caracteres.")]
    public string Contrasena { get; set; } = "";
}

public class DatosRegistro
{
    private string nombre = "", apellido = "", email = "", telefono = "";
    [Required(ErrorMessage = "Ingresá tu nombre."), StringLength(100)]
    public string Nombre { get => nombre; set => nombre = value?.Trim() ?? ""; }
    [Required(ErrorMessage = "Ingresá tu apellido."), StringLength(100)]
    public string Apellido { get => apellido; set => apellido = value?.Trim() ?? ""; }
    [Required(ErrorMessage = "Ingresá tu correo electrónico."), EmailAddress(ErrorMessage = "Ingresá un correo válido."), StringLength(254)]
    public string Email { get => email; set => email = value?.Trim() ?? ""; }
    [Required(ErrorMessage = "Ingresá tu teléfono."), StringLength(40)]
    public string Telefono { get => telefono; set => telefono = value?.Trim() ?? ""; }
    [Required(ErrorMessage = "Ingresá una contraseña."), StringLength(128, MinimumLength = 12, ErrorMessage = "La contraseña debe tener entre 12 y 128 caracteres.")]
    public string Contrasena { get; set; } = "";
    [StringLength(200)] public string? RazonSocial { get; set; }
    [StringLength(20)] public string? Cuit { get; set; }
    [StringLength(300)] public string? DireccionPrincipal { get; set; }
}

public record RespuestaLogin(string AccessToken, DateTimeOffset ExpiraEn, string EstadoCuenta, string? Mensaje);
public class CuentaActual
{
    public int? IdUsuario { get; set; }
    public string? Nombre { get; set; }
    public string? Apellido { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Rol { get; set; }
    public string EstadoCuenta { get; set; } = "";
    public string? Mensaje { get; set; }
}

public interface IAlmacenSesion
{
    Task<string?> LeerAsync();
    Task GuardarAsync(string token);
    Task BorrarAsync();
}
