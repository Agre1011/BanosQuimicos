using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BanosQuimicos.Api.Contratos.Banos;

public record EmpleadoAsignableRespuesta(int IdUsuario, string Nombre);
public record EstadoBanoRespuesta(int IdBano, string Codigo, string TipoBano, string Condicion,
    bool MantenimientoBloqueante, bool Disponible, int? IdResponsable, string? Responsable,
    bool ResponsableActivo, int? IdServicio, string? EstadoServicio, string? TipoUso,
    string? Cliente, int? IdUbicacion, string? Direccion, int? LimpiezasSemana);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class AsignarResponsableSolicitud
{
    [Range(1, int.MaxValue)] public int IdEmpleado { get; set; }
    public int? IdResponsableAnterior { get; set; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class ProgramarLimpiezaSolicitud
{
    public Guid ClaveProgramacion { get; set; }
    [Range(1, int.MaxValue)] public int IdEmpleado { get; set; }
    [Range(1, int.MaxValue)] public int IdServicio { get; set; }
    [Range(1, int.MaxValue)] public int IdUbicacion { get; set; }
    public DateOnly Fecha { get; set; }
}
