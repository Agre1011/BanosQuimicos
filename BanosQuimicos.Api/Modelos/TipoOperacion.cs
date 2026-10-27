namespace BanosQuimicos.Api.Modelos;

public class TipoOperacion
{
    public int IdTipoOperacion { get; set; }
    public string NombreOperacion { get; set; } = string.Empty;
    public bool AplicaASolicitudes { get; set; }
    public bool AplicaATareas { get; set; }
    public string? Descripcion { get; set; }
}
