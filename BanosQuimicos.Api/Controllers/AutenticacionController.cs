using BanosQuimicos.Api.Contratos.Autenticacion;
using BanosQuimicos.Api.Datos;
using BanosQuimicos.Api.Modelos;
using BanosQuimicos.Api.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BanosQuimicos.Api.Controllers;

[ApiController]
[Route("api/autenticacion")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AutenticacionController(BanosQuimicosDbContext db, IPasswordHasher<Usuario> hasher,
    EmisorTokens tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("registro")]
    public async Task<IActionResult> Registrar(RegistroClienteSolicitud datos, CancellationToken ct)
    {
        if (await db.Usuarios.AnyAsync(x => x.Email == datos.Email, ct))
            return Conflict(new { mensaje = "El correo electrónico ya está registrado." });
        var rol = await db.Roles.SingleAsync(x => x.NombreRol == "Cliente", ct);
        var usuario = new Usuario {
            Nombre = datos.Nombre, Apellido = datos.Apellido, Email = datos.Email,
            Telefono = datos.Telefono, IdRol = rol.IdRol, EstadoCuenta = "Activa",
            FechaRegistro = DateTimeOffset.UtcNow
        };
        usuario.ContrasenaHash = hasher.HashPassword(usuario, datos.Contrasena);
        var cliente = new Cliente { Usuario = usuario, RazonSocial = Limpiar(datos.RazonSocial),
            Cuit = Limpiar(datos.Cuit), DireccionPrincipal = Limpiar(datos.DireccionPrincipal) };
        // Si falla el perfil o la cuenta, no puede quedar solo una mitad del registro.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // El índice único resuelve también dos registros concurrentes del mismo correo.
            await transaction.RollbackAsync(ct);
            return Conflict(new { mensaje = "El correo electrónico ya está registrado." });
        }
        return StatusCode(StatusCodes.Status201Created, new RegistroRespuesta("Activa", "Cuenta creada. Ya podés iniciar sesión."));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginSolicitud datos, CancellationToken ct)
    {
        var usuario = await db.Usuarios.Include(x => x.Rol).SingleOrDefaultAsync(x => x.Email == datos.Email, ct);
        if (usuario is null)
            return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });
        var resultado = hasher.VerifyHashedPassword(usuario, usuario.ContrasenaHash, datos.Contrasena);
        if (resultado == PasswordVerificationResult.Failed)
            return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });
        if (usuario.EstadoCuenta is not ("Pendiente" or "Activa"))
            return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = "La cuenta no está habilitada." });
        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.ContrasenaHash = hasher.HashPassword(usuario, datos.Contrasena);
            await db.SaveChangesAsync(ct);
        }
        return Ok(tokens.Crear(usuario));
    }

    [Authorize(Policy = Politicas.CuentaIdentificada)]
    [HttpGet("yo")]
    public IActionResult Yo()
    {
        var usuario = (Usuario)HttpContext.Items[Politicas.CuentaActual]!;
        if (usuario.EstadoCuenta == "Pendiente")
            return Ok(new EsperaRespuesta("Pendiente", Politicas.AvisoEspera));
        return Ok(new UsuarioRespuesta(usuario.IdUsuario, usuario.Nombre, usuario.Apellido,
            usuario.Email, usuario.Telefono, usuario.Rol.NombreRol, usuario.EstadoCuenta));
    }

    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
