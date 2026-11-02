namespace BanosQuimicos.App.Autenticacion;

public record CuentaGestion(int IdUsuario, string Nombre, string Apellido, string Email,
    string Telefono, string Rol, string EstadoCuenta, DateTimeOffset FechaRegistro, bool PuedeActivar);
public record PaginaCuentas(List<CuentaGestion> Elementos, int Pagina, int TamanoPagina, int Total);
