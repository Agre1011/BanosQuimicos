namespace BanosQuimicos.App.Autenticacion;

public record EmpleadoAsignable(int IdUsuario, string Nombre);
public record EstadoBano(int IdBano, string Codigo, string TipoBano, string Condicion,
    bool MantenimientoBloqueante, bool Disponible, int? IdResponsable, string? Responsable,
    bool ResponsableActivo, int? IdServicio, string? EstadoServicio, string? TipoUso,
    string? Cliente, int? IdUbicacion, string? Direccion, int? LimpiezasSemana);
public record LimpiezaProgramada(int IdTarea);
public record ProgramarLimpieza(Guid ClaveProgramacion, int IdEmpleado, int IdServicio, int IdUbicacion, DateOnly Fecha);
