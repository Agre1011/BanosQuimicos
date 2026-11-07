using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BanosQuimicos.Api.Contratos.Solicitudes;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class AsignarSolicitud
{
    public Guid ClaveProgramacion { get; set; }
    [Range(1, int.MaxValue)] public int IdEmpleado { get; set; }
    [Range(1, 2)] public int IdEstadoAnterior { get; set; }
    [Range(1, int.MaxValue)] public int IdServicioEsperado { get; set; }
    [Range(1, int.MaxValue)] public int IdUbicacionEsperada { get; set; }
    public int? IdTipoTareaReclamo { get; set; }
    public bool BloqueaAsignacion { get; set; }
    public DateOnly Fecha { get; set; }
}
