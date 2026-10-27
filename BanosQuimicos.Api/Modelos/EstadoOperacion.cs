namespace BanosQuimicos.Api.Modelos;

public class EstadoOperacion
{
    public int IdEstadoOperacion { get; set; }
    public string NombreEstado { get; set; } = string.Empty;
    public string Ambito { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
