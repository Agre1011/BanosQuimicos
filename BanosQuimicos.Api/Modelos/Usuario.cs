namespace BanosQuimicos.Api.Modelos;

public class Usuario
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    private string email = string.Empty;

    // El correo es la única credencial: se eliminan espacios exteriores al asignarlo.
    public string Email { get => email; set => email = value.Trim(); }
    public string Telefono { get; set; } = string.Empty;
    public string ContrasenaHash { get; set; } = string.Empty;
    public int IdRol { get; set; }
    public string EstadoCuenta { get; set; } = string.Empty;
    public DateTimeOffset FechaRegistro { get; set; }
    public string? MotivoSuspension { get; set; }
    public DateTimeOffset? FechaBaja { get; set; }
    public Rol Rol { get; set; } = null!;
    public Cliente? Cliente { get; set; }
}
