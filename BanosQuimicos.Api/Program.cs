using BanosQuimicos.Api.Datos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<BanosQuimicosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BanosQuimicos")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:BanosQuimicos.")));

var app = builder.Build();
// Las migraciones se aplican explícitamente con dotnet ef, nunca al iniciar la API.
app.MapControllers();
app.Run();
