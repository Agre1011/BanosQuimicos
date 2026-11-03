using System.ComponentModel.DataAnnotations;

namespace BanosQuimicos.Api.Contratos.Cuentas;

public class ConsultaCuentas
{
    [Range(1, int.MaxValue)] public int Pagina { get; set; } = 1;
    [Range(1, 50)] public int TamanoPagina { get; set; } = 10;
    [StringLength(254)] public string? Busqueda { get; set; }
    [RegularExpression("^(Cliente|Empleado)$")] public string? Rol { get; set; }
    [RegularExpression("^(Pendiente|Activa|Suspendida|BajaLogica)$")]
    public string? Estado { get; set; }
}

// DTOs explícitos: nunca serializar Usuario ni su hash de contraseña.
public record CuentaGestionRespuesta(int IdUsuario, string Nombre, string Apellido,
    string Email, string Telefono, string Rol, string EstadoCuenta,
    DateTimeOffset FechaRegistro, bool PuedeActivar);
public record PaginaCuentasRespuesta(List<CuentaGestionRespuesta> Elementos,
    int Pagina, int TamanoPagina, int Total);

[System.Text.Json.Serialization.JsonUnmappedMemberHandling(System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
public class CambioRolSolicitud
{
    [Required, RegularExpression("^(Cliente|Empleado)$")] public string RolAnterior { get; set; } = "";
    [Required, RegularExpression("^(Cliente|Empleado)$")] public string NuevoRol { get; set; } = "";
}
