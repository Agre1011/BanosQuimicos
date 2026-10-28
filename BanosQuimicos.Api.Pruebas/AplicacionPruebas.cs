using System.Security.Cryptography;
using BanosQuimicos.Api.Contratos.Autenticacion;
using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Modelos;
using BanosQuimicos.Api.Seguridad;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BanosQuimicos.Api.Pruebas;

public class AplicacionPruebas : WebApplicationFactory<Program>
{
    public string NombreBase { get; } = "BanosQuimicosAuthPruebas_" + Guid.NewGuid().ToString("N");
    public string ClaveJwt { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    public string ClaveAdmin { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
    public string CorreoAdmin { get; } = "prueba-admin-" + Guid.NewGuid().ToString("N") + "@example.invalid";
    public string Conexion => new SqlConnectionStringBuilder {
        DataSource = @"(localdb)\MSSQLLocalDB",
        InitialCatalog = NombreBase,
        IntegratedSecurity = true, TrustServerCertificate = true
    }.ConnectionString;

    public AplicacionPruebas()
    {
        // Nunca usar BanosQuimicosDb ni borrar una base previa. Cada ejecución usa un nombre nuevo.
        using var master = new SqlConnection(new SqlConnectionStringBuilder(Conexion) { InitialCatalog = "master" }.ConnectionString);
        master.Open();
        using var cmd = new SqlCommand("SELECT DB_ID(@nombre)", master);
        cmd.Parameters.AddWithValue("@nombre", NombreBase);
        if (cmd.ExecuteScalar() is not DBNull) throw new InvalidOperationException("La base de prueba ya existe.");
        using var db = NuevoContexto();
        db.Database.Migrate();
        new PrimerAdministrador(db, new PasswordHasher<Usuario>()).CrearAsync(new RegistroClienteSolicitud {
            Nombre = "PRUEBA AUTENTICACION", Apellido = "Administrador", Email = CorreoAdmin,
            Telefono = "+00 PRUEBA", Contrasena = ClaveAdmin
        }).GetAwaiter().GetResult();
    }

    public BanosQuimicosDbContext NuevoContexto() => new(new DbContextOptionsBuilder<BanosQuimicosDbContext>()
        .UseSqlServer(Conexion).Options);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:BanosQuimicos"] = Conexion,
            ["Jwt:ClaveBase64"] = ClaveJwt
        }));
        builder.ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Warning));
        // Rutas de comprobación cargadas solo en TestServer, nunca en la API distribuida.
        builder.ConfigureServices(s => s.AddControllers().AddApplicationPart(typeof(ControlPruebasController).Assembly));
    }
    // No se llama EnsureDeleted: se conserva la base claramente identificada para inspección.
}
