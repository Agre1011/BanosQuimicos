using System.Data;
using BanosQuimicos.Api.Contratos.Autenticacion;
using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Modelos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Seguridad;

public class PrimerAdministrador(BanosQuimicosDbContext db, IPasswordHasher<Usuario> hasher)
{
    public async Task CrearAsync(RegistroClienteSolicitud datos, CancellationToken ct = default)
    {
        var errores = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(datos,
                new System.ComponentModel.DataAnnotations.ValidationContext(datos), errores, true))
            throw new InvalidOperationException("Revisar nombre, apellido, correo, teléfono y contraseña de 12 a 128 caracteres.");
        // Serializable impide que dos ejecuciones simultáneas creen dos primeros administradores.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var rol = await db.Roles.SingleAsync(x => x.NombreRol == "Administrador", ct);
        if (await db.Usuarios.AnyAsync(x => x.IdRol == rol.IdRol, ct))
            throw new InvalidOperationException("Ya existe un administrador. No se modificó ninguna cuenta.");
        if (await db.Usuarios.AnyAsync(x => x.Email == datos.Email, ct))
            throw new InvalidOperationException("Ese correo ya existe. No se convierte una cuenta en administrador.");
        var usuario = new Usuario {
            Nombre = datos.Nombre, Apellido = datos.Apellido, Email = datos.Email, Telefono = datos.Telefono,
            IdRol = rol.IdRol, EstadoCuenta = "Activa", FechaRegistro = DateTimeOffset.UtcNow
        };
        usuario.ContrasenaHash = hasher.HashPassword(usuario, datos.Contrasena);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public static async Task EjecutarConsolaAsync(IServiceProvider services, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("La creación local del primer administrador solo se permite en Development.");
        using var scope = services.CreateScope();
        string Leer(string campo) { Console.Write(campo + ": "); return Console.ReadLine() ?? ""; }
        var datos = new RegistroClienteSolicitud {
            Nombre = Leer("Nombre"), Apellido = Leer("Apellido"), Email = Leer("Correo electrónico"), Telefono = Leer("Teléfono")
        };
        Console.Write("Contraseña (12 a 128 caracteres, no se muestra): ");
        datos.Contrasena = LeerContrasena();
        Console.Write("Repetir contraseña: ");
        if (datos.Contrasena != LeerContrasena()) throw new InvalidOperationException("Las contraseñas no coinciden.");
        await scope.ServiceProvider.GetRequiredService<PrimerAdministrador>().CrearAsync(datos);
        Console.WriteLine("Administrador local creado. No se inició el servidor web.");
    }

    private static string LeerContrasena()
    {
        if (Console.IsInputRedirected) throw new InvalidOperationException("Usar una terminal interactiva; no enviar contraseñas por argumentos ni archivos.");
        var texto = new System.Text.StringBuilder();
        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter) { Console.WriteLine(); return texto.ToString(); }
            if (tecla.Key == ConsoleKey.Backspace) { if (texto.Length > 0) texto.Length--; }
            else if (!char.IsControl(tecla.KeyChar)) texto.Append(tecla.KeyChar);
        }
    }
}
