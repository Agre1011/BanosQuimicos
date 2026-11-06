namespace BanosQuimicos.Api.Modelos;

public class TareaOperativa
{
    public int IdTarea { get; set; }
    // Identifica una programación manual para que reenviarla no cree otra tarea.
    public Guid? ClaveProgramacion { get; set; }
    public int IdBano { get; set; }
    public int IdTipoOperacion { get; set; }
    public int IdEstadoOperacion { get; set; }
    public DateTimeOffset FechaRegistro { get; set; }
    public bool BloqueaAsignacion { get; set; }
    public int? IdServicio { get; set; }
    public int? IdEmpleado { get; set; }
    public int? IdSolicitud { get; set; }
    public int? IdUbicacion { get; set; }
    public int? IdUbicacionDestino { get; set; }
    public DateTimeOffset? FechaProgramada { get; set; }
    public DateTimeOffset? FechaInicioReal { get; set; }
    public DateTimeOffset? FechaRealizada { get; set; }
    // Cumplimiento registrado por el empleado; todavía no implica cierre de la tarea.
    public DateTimeOffset? FechaTrabajoRegistrado { get; set; }
    public int? IdEmpleadoRegistroTrabajo { get; set; }
    public Usuario? EmpleadoRegistroTrabajo { get; set; }
    public DateTimeOffset? FechaCancelacion { get; set; }
    public bool? PapelHigienicoRepuesto { get; set; }
    public string? ObservacionesEmpleado { get; set; }
    public BanoQuimico Bano { get; set; } = null!;
    public TipoOperacion TipoOperacion { get; set; } = null!;
    public EstadoOperacion EstadoOperacion { get; set; } = null!;
    public Servicio? Servicio { get; set; }
    public Usuario? Empleado { get; set; }
    public SolicitudCliente? Solicitud { get; set; }
    public UbicacionServicio? Ubicacion { get; set; }
    public UbicacionServicio? UbicacionDestino { get; set; }
}
