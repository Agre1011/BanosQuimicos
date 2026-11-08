namespace BanosQuimicos.App.Autenticacion;

public record SolicitudResumen(int IdSolicitud, string Cliente, string Tipo, string Estado, DateTimeOffset FechaSolicitud);
public record TrabajoSolicitud(int IdTarea, string Tipo, string Estado, string? Empleado, DateTimeOffset? FechaProgramada,
    string? CodigoBano = null, int? IdServicio = null);
public record SolicitudDetalle(int IdSolicitud, string Cliente, int IdTipoOperacion, string Tipo,
    int IdEstadoOperacion, string Estado, DateTimeOffset FechaSolicitud, string? Descripcion,
    int? IdServicioReferencia, string? EstadoServicio, string? CodigoBano, string? Direccion,
    string? NuevaDireccion, string? ModalidadCambioDireccion, DateTimeOffset? FechaDeseada,
    DateTimeOffset? FechaCambioEfectiva, DateTimeOffset? FechaResolucion, string? ObservacionesAdministrador,
    int? CantidadBanos, string? TipoBanoSolicitado, string? TipoUsoSolicitado, string? DireccionSolicitada,
    List<TrabajoSolicitud> Trabajos, int? IdUbicacionActual, List<ServicioSolicitud>? Servicios = null);
public record AsignacionSolicitud(Guid ClaveProgramacion, int IdEmpleado, int IdEstadoAnterior,
    int IdServicioEsperado, int IdUbicacionEsperada, int? IdTipoTareaReclamo, bool BloqueaAsignacion, DateOnly Fecha);
public record BanoDisponible(int IdBano, string Codigo, string Tipo);
public record ServicioSolicitud(int IdServicio, int IdBano, string CodigoBano, string Estado,
    string TipoUso, int LimpiezasSemana, string? Direccion);
public record AsignacionBanos(Guid ClaveProgramacion, List<int> IdBanos, int IdEmpleado,
    int IdEstadoAnterior, int? CantidadLimpiezasSemana, DateOnly Fecha);

public record ResolverReclamo(string Respuesta, int IdEstadoAnterior);
public class BorradorRespuestaReclamo
{
    public string Texto { get; set; } = "";
    public ResolverReclamo? Envio { get; set; }
}
