using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BanosQuimicos.Api.Contratos.Banos;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class AltaBanoSolicitud
{
    public Guid ClaveAlta { get; set; }
    [Range(1, int.MaxValue)] public int IdTipoBano { get; set; }
}
public class CondicionBanoSolicitud
{
    [Required, RegularExpression("^(Apto|No apto)$")] public string Anterior { get; set; } = "";
    [Required, RegularExpression("^(Apto|No apto)$")] public string Nueva { get; set; } = "";
}
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class MantenimientoBanoSolicitud
{
    public Guid ClaveProgramacion { get; set; }
    [Range(1, int.MaxValue)] public int IdEmpleado { get; set; }
    public DateOnly Fecha { get; set; }
    [Required, StringLength(2000)] public string Descripcion { get; set; } = "";
    public bool BloqueaAsignacion { get; set; }
}
