using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BanosQuimicos.Api.Contratos.Servicios;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ProgramarRetiroSolicitud
{
    public Guid ClaveProgramacion { get; set; }
    [Range(1, int.MaxValue)] public int IdEmpleado { get; set; }
    [Range(1, int.MaxValue)] public int IdUbicacionEsperada { get; set; }
    [Range(1, int.MaxValue)] public int? IdSolicitudBaja { get; set; }
    [Range(1, 2)] public int? IdEstadoSolicitudAnterior { get; set; }
    public DateOnly Fecha { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ResolverPendientesSolicitud
{
    [Required] public List<int> IdCancelar { get; set; } = [];
    [Required] public List<int> IdConservar { get; set; } = [];
}
