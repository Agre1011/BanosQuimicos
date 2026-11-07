using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BanosQuimicos.Api.Contratos.Solicitudes;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class AsignarBanosSolicitud
{
    public Guid ClaveProgramacion { get; set; }
    [Required, MinLength(1)] public List<int> IdBanos { get; set; } = [];
    [Range(1, int.MaxValue)] public int IdEmpleado { get; set; }
    [Range(1, 2)] public int IdEstadoAnterior { get; set; }
    [Required, Range(0, int.MaxValue)] public int? CantidadLimpiezasSemana { get; set; }
    public DateOnly Fecha { get; set; }
}

public record BanoDisponibleRespuesta(int IdBano, string Codigo, string Tipo);
public record ServicioSolicitudRespuesta(int IdServicio, int IdBano, string CodigoBano,
    string Estado, string TipoUso, int LimpiezasSemana, string? Direccion);
