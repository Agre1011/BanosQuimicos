namespace BanosQuimicos.Api.Contratos.Tareas;

public record TareaAsignadaRespuesta(int IdTarea, string Tipo, string CodigoBano,
    string? Direccion, DateTimeOffset? FechaProgramada, string Estado, string? Cliente);
public record ResumenTareasRespuesta(DateOnly Fecha, int Cantidad);
