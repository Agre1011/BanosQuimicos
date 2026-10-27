var builder = WebApplication.CreateBuilder(args);

// Registra los controladores que atienden las rutas de la API.
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
