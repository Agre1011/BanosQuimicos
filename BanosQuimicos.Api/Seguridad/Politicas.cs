namespace BanosQuimicos.Api.Seguridad;

public static class Politicas
{
    public const string CuentaIdentificada = "CuentaIdentificada";
    public const string CuentaActiva = "CuentaActiva";
    public const string Empleado = "Empleado";
    public const string Administrador = "Administrador";
    public const string EstadoClaim = "estado_cuenta";
    public const string CuentaActual = "CuentaActual";
    public const string AvisoEspera = "Tu cuenta está pendiente de activación por el administrador.";
}
