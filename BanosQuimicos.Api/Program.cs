using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Seguridad;
using Microsoft.EntityFrameworkCore;

var crearAdministrador = args.Contains("--crear-admin-local");
var builder = WebApplication.CreateBuilder(args.Where(x => x != "--crear-admin-local").ToArray());

builder.Services.AddControllers();
builder.Services.AddDbContext<BanosQuimicosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BanosQuimicos")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:BanosQuimicos.")));
builder.Services.AgregarSeguridad(builder.Configuration);

var app = builder.Build();
if (crearAdministrador)
{
    await PrimerAdministrador.EjecutarConsolaAsync(app.Services, app.Environment);
    return;
}
// Migraciones explícitas: arrancar la API no cambia el esquema ni crea usuarios.
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Permite alojar la API real en las pruebas de integración.
public partial class Program { }
