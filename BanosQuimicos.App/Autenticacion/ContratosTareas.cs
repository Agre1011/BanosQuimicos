namespace BanosQuimicos.App.Autenticacion;

public record TareaAsignada(int IdTarea, string Tipo, string CodigoBano, string? Direccion,
    DateTimeOffset? FechaProgramada, string Estado, string? Cliente,
    string? TipoSolicitud = null, string? DescripcionSolicitud = null,
    string? ObservacionesServicio = null, string? ObservacionesEmpleado = null,
    bool? PapelHigienicoRepuesto = null, DateTimeOffset? FechaTrabajoRegistrado = null,
    int? IdEmpleadoRegistroTrabajo = null, string? EmpleadoRegistroTrabajo = null,
    DateTimeOffset? FechaRealizada = null, bool PuedeRegistrar = false, bool PuedeFinalizar = false,
    string? ObservacionesSolicitud = null);
public record ResumenTareas(DateOnly Fecha, int Cantidad);
