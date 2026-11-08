namespace BanosQuimicos.Api.Contratos.Autenticacion;

public record UsuarioRespuesta(int IdUsuario, string Nombre, string Apellido, string Email,
    string Telefono, string Rol, string EstadoCuenta);
public record RegistroRespuesta(string EstadoCuenta, string Mensaje);
public record EsperaRespuesta(string EstadoCuenta, string Mensaje);
public record LoginRespuesta(string AccessToken, DateTimeOffset ExpiraEn, string EstadoCuenta, string? Mensaje);
