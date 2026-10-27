namespace BanosQuimicos.Api.Modelos;

public class Cliente
{
    public int IdCliente { get; set; }
    public int IdUsuario { get; set; }
    public string? RazonSocial { get; set; }
    public string? Cuit { get; set; }
    public string? DireccionPrincipal { get; set; }
    public string? Observaciones { get; set; }
    public Usuario Usuario { get; set; } = null!;
}
