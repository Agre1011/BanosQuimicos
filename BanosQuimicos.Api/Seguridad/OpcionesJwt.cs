namespace BanosQuimicos.Api.Seguridad;

public class OpcionesJwt
{
    public string Emisor { get; set; } = "BanosQuimicos.Api";
    public string Audiencia { get; set; } = "BanosQuimicos.App";
    public int MinutosVigencia { get; set; } = 30;
    public string ClaveBase64 { get; set; } = "";

    public static bool ClaveValida(OpcionesJwt opciones)
    {
        try { return Convert.FromBase64String(opciones.ClaveBase64).Length >= 32; }
        catch (FormatException) { return false; }
    }
}
