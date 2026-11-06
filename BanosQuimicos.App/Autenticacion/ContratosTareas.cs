namespace BanosQuimicos.App.Autenticacion;

public record TareaAsignada(int IdTarea, string Tipo, string CodigoBano, string? Direccion,
    DateTimeOffset? FechaProgramada, string Estado, string? Cliente);
public record ResumenTareas(DateOnly Fecha, int Cantidad);
