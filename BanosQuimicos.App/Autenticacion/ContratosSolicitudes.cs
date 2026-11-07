namespace BanosQuimicos.App.Autenticacion;

public record SolicitudResumen(int IdSolicitud, string Cliente, string Tipo, string Estado, DateTimeOffset FechaSolicitud);
public record TrabajoSolicitud(int IdTarea, string Tipo, string Estado, string? Empleado, DateTimeOffset? FechaProgramada);
public record SolicitudDetalle(int IdSolicitud, string Cliente, int IdTipoOperacion, string Tipo,
    int IdEstadoOperacion, string Estado, DateTimeOffset FechaSolicitud, string? Descripcion,
    int? IdServicioReferencia, string? EstadoServicio, string? CodigoBano, string? Direccion,
    string? NuevaDireccion, string? ModalidadCambioDireccion, DateTimeOffset? FechaDeseada,
    DateTimeOffset? FechaCambioEfectiva, DateTimeOffset? FechaResolucion, string? ObservacionesAdministrador,
    int? CantidadBanos, string? TipoBanoSolicitado, string? TipoUsoSolicitado, string? DireccionSolicitada,
    List<TrabajoSolicitud> Trabajos, int? IdUbicacionActual);
public record AsignacionSolicitud(Guid ClaveProgramacion, int IdEmpleado, int IdEstadoAnterior,
    int IdServicioEsperado, int IdUbicacionEsperada, int? IdTipoTareaReclamo, bool BloqueaAsignacion, DateOnly Fecha);
