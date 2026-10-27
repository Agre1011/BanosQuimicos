namespace BanosQuimicos.Api.Modelos;

public class EstadoBano
{
    public int IdEstadoBano { get; set; }
    public string NombreEstado { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}
