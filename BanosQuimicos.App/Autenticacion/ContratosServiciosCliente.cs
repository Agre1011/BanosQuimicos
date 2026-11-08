namespace BanosQuimicos.App.Autenticacion;

public record ResumenServiciosCliente(int Cantidad);
public record ServicioCliente(int IdServicio, string CodigoBano, string TipoBano, string TipoUso,
    string? Direccion, int CantidadLimpiezasSemana, string EstadoServicio, int? IdUbicacionActual = null);
