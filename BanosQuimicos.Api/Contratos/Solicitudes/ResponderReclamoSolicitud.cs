using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BanosQuimicos.Api.Contratos.Solicitudes;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ResponderReclamoSolicitud
{
    [Required, StringLength(2000)] public string Respuesta { get; set; } = "";
    [Range(1, 2)] public int IdEstadoAnterior { get; set; }
}
