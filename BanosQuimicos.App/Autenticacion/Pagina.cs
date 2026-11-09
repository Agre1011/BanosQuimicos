namespace BanosQuimicos.App.Autenticacion;

public record Pagina<T>(List<T> Elementos,
    [property: System.Text.Json.Serialization.JsonPropertyName("pagina")] int PaginaActual,
    int TamanoPagina, int Total);
