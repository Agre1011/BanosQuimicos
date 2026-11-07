using System.ComponentModel.DataAnnotations;

namespace BanosQuimicos.Api.Contratos.Solicitudes;

public class SolicitudServicioSolicitud
{
    public Guid ClaveEnvio { get; set; }
    [StringLength(2000)] public string? Descripcion { get; set; }
}
