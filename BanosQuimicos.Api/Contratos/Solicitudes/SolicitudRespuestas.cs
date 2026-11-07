namespace BanosQuimicos.Api.Contratos.Solicitudes;

public record SolicitudResumenRespuesta(int IdSolicitud, string Cliente, string Tipo, string Estado, DateTimeOffset FechaSolicitud);
public record TrabajoSolicitudRespuesta(int IdTarea, string Tipo, string Estado, string? Empleado, DateTimeOffset? FechaProgramada);
public record SolicitudDetalleRespuesta(int IdSolicitud, string Cliente, int IdTipoOperacion, string Tipo,
    int IdEstadoOperacion, string Estado, DateTimeOffset FechaSolicitud, string? Descripcion,
    int? IdServicioReferencia, string? EstadoServicio, string? CodigoBano, string? Direccion,
    string? NuevaDireccion, string? ModalidadCambioDireccion, DateTimeOffset? FechaDeseada,
    DateTimeOffset? FechaCambioEfectiva, DateTimeOffset? FechaResolucion, string? ObservacionesAdministrador,
    int? CantidadBanos, string? TipoBanoSolicitado, string? TipoUsoSolicitado, string? DireccionSolicitada,
    List<TrabajoSolicitudRespuesta> Trabajos, int? IdUbicacionActual);
