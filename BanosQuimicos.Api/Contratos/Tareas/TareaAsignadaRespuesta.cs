namespace BanosQuimicos.Api.Contratos.Tareas;

public record TareaAsignadaRespuesta(int IdTarea, string Tipo, string CodigoBano,
    string? Direccion, DateTimeOffset? FechaProgramada, string Estado, string? Cliente,
    string? TipoSolicitud = null, string? DescripcionSolicitud = null,
    string? ObservacionesServicio = null, string? ObservacionesEmpleado = null,
    bool? PapelHigienicoRepuesto = null, DateTimeOffset? FechaTrabajoRegistrado = null,
    int? IdEmpleadoRegistroTrabajo = null, string? EmpleadoRegistroTrabajo = null,
    DateTimeOffset? FechaRealizada = null, bool PuedeRegistrar = false, bool PuedeFinalizar = false,
    string? ObservacionesSolicitud = null, int? IdServicio = null, int? IdSolicitud = null,
    int? IdUbicacionOrigen = null, int? IdUbicacionDestino = null, string? DireccionDestino = null,
    List<ImpedimentoRetiroRespuesta>? ImpedimentosRetiro = null, string? DescripcionTrabajo = null, string? CondicionBano = null,
    DateTimeOffset? FechaBajaBano = null, int OtrosMantenimientosBloqueantes = 0)
{
    public string? DireccionOrigen => Direccion;
}
public record ImpedimentoRetiroRespuesta(int IdTarea, string Tipo, string Estado, bool TrabajoRegistrado);
public record ResumenTareasRespuesta(DateOnly Fecha, int Cantidad);
