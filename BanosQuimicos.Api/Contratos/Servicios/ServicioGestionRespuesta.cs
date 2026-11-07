namespace BanosQuimicos.Api.Contratos.Servicios;

public record TrabajoServicioRespuesta(int IdTarea, int IdTipoOperacion, string Tipo, int IdEstadoOperacion,
    string Estado, string? Empleado, DateTimeOffset? FechaProgramada, int? IdSolicitud,
    bool BloqueaAsignacion, DateTimeOffset? FechaTrabajoRegistrado);
public record BajaServicioRespuesta(int IdSolicitud, int IdEstadoOperacion, string Estado, string? Descripcion);
public record ServicioGestionRespuesta(int IdServicio, int IdBano, string CodigoBano, string TipoBano, string TipoUso,
    string Cliente, string? Direccion, int LimpiezasSemana, string Estado, string? ResponsableHabitual,
    string CondicionBano, bool MantenimientoBloqueante, bool DisponibleAhora, int? IdUbicacionActual,
    DateTimeOffset? FechaEntregaReal, DateTimeOffset? FechaRetiroReal, DateTimeOffset? FechaFinalizacion,
    List<TrabajoServicioRespuesta> Trabajos, List<BajaServicioRespuesta> SolicitudesBaja);
