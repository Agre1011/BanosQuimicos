namespace BanosQuimicos.App.Autenticacion;

public record EmpleadoAsignable(int IdUsuario, string Nombre);
public record EstadoBano(int IdBano, string Codigo, string TipoBano, string Condicion,
    bool MantenimientoBloqueante, bool Disponible, int? IdResponsable, string? Responsable,
    bool ResponsableActivo, int? IdServicio, string? EstadoServicio, string? TipoUso,
    string? Cliente, int? IdUbicacion, string? Direccion, int? LimpiezasSemana, DateTimeOffset? FechaBaja = null);
public record LimpiezaProgramada(int IdTarea);
public record ProgramarLimpieza(Guid ClaveProgramacion, int IdEmpleado, int IdServicio, int IdUbicacion, DateOnly Fecha);

public record AltaInventario(Guid ClaveAlta, int IdTipoBano);
public record CondicionInventario(string Anterior, string Nueva);
public record MantenimientoInventario(Guid ClaveProgramacion, int IdEmpleado, DateOnly Fecha, string Descripcion, bool BloqueaAsignacion);
// Durante la sesión, recuperar un envío incierto con la misma clave evita duplicarlo.
public class BorradorInventario
{
    public int Tipo { get; set; }
    public AltaInventario? Alta { get; set; }
    public Dictionary<int, MantenimientoInventario> Mantenimientos { get; } = [];
}
