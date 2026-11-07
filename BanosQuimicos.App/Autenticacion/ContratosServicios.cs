namespace BanosQuimicos.App.Autenticacion;

public record TrabajoServicio(int IdTarea, int IdTipoOperacion, string Tipo, int IdEstadoOperacion,
    string Estado, string? Empleado, DateTimeOffset? FechaProgramada, int? IdSolicitud,
    bool BloqueaAsignacion, DateTimeOffset? FechaTrabajoRegistrado);
public record BajaServicio(int IdSolicitud, int IdEstadoOperacion, string Estado, string? Descripcion);
public record ServicioGestion(int IdServicio, int IdBano, string CodigoBano, string TipoBano, string TipoUso,
    string Cliente, string? Direccion, int LimpiezasSemana, string Estado, string? ResponsableHabitual,
    string CondicionBano, bool MantenimientoBloqueante, bool DisponibleAhora, int? IdUbicacionActual,
    DateTimeOffset? FechaEntregaReal, DateTimeOffset? FechaRetiroReal, DateTimeOffset? FechaFinalizacion,
    List<TrabajoServicio> Trabajos, List<BajaServicio> SolicitudesBaja);
public record ProgramarRetiro(Guid ClaveProgramacion, int IdEmpleado, int IdUbicacionEsperada,
    int? IdSolicitudBaja, int? IdEstadoSolicitudAnterior, DateOnly Fecha);
public record ResolverPendientes(List<int> IdCancelar, List<int> IdConservar);
