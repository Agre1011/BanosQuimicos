using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using BanosQuimicos.Api.Contratos.Autenticacion;
using BanosQuimicos.Api.Modelos;
using BanosQuimicos.Api.Seguridad;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using BanosQuimicos.Api.Datos;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BanosQuimicos.Api.Pruebas;

public class AutenticacionPruebas(AplicacionPruebas app, ITestOutputHelper salida) : IClassFixture<AplicacionPruebas>
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private RegistroClienteSolicitud Datos() => new() {
        Nombre = "PRUEBA AUTENTICACION", Apellido = "Cliente", Email = "prueba-" + Guid.NewGuid().ToString("N") + "@example.invalid",
        Telefono = "+00 123 PRUEBA", Contrasena = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
    };
    private HttpClient Cliente(string? token = null)
    {
        var client = app.CreateClient();
        if (token is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
    private async Task<(RegistroClienteSolicitud Datos, int Id)> Registrar()
    {
        var datos = Datos();
        using var http = Cliente();
        var respuesta = await http.PostAsJsonAsync("/api/autenticacion/registro", datos, Ct);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        using var db = app.NuevoContexto();
        return (datos, await db.Usuarios.Where(x => x.Email == datos.Email).Select(x => x.IdUsuario).SingleAsync(Ct));
    }
    private async Task<string> Token(string email, string clave)
    {
        using var http = Cliente();
        var respuesta = await http.PostAsJsonAsync("/api/autenticacion/login", new { email, contrasena = clave }, Ct);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<LoginRespuesta>(Ct))!.AccessToken;
    }
    private Task<string> TokenAdmin() => Token(app.CorreoAdmin, app.ClaveAdmin);

    [Fact]
    public async Task Registro_normaliza_correo_y_crea_perfil_con_hash_sin_exponerlo()
    {
        salida.WriteLine("Base aislada conservada: " + app.NombreBase);
        var datos = Datos();
        var correo = datos.Email;
        datos.Email = "  " + correo.ToUpperInvariant() + "  ";
        using var http = Cliente();
        var r = await http.PostAsJsonAsync("/api/autenticacion/registro", datos, Ct);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var json = await r.Content.ReadAsStringAsync(Ct);
        Assert.DoesNotContain("hash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(datos.Contrasena, json);
        using var db = app.NuevoContexto();
        var usuario = await db.Usuarios.Include(x => x.Rol).Include(x => x.Cliente).SingleAsync(x => x.Email == correo, Ct);
        Assert.Equal(correo.ToUpperInvariant(), usuario.Email);
        Assert.Equal("Cliente", usuario.Rol.NombreRol);
        Assert.Equal("Pendiente", usuario.EstadoCuenta);
        Assert.NotNull(usuario.Cliente);
        Assert.Null(usuario.Cliente.RazonSocial);
        Assert.Equal(datos.Telefono, usuario.Telefono);
        Assert.NotEqual(datos.Contrasena, usuario.ContrasenaHash);
        Assert.NotEqual(PasswordVerificationResult.Failed,
            new PasswordHasher<Usuario>().VerifyHashedPassword(usuario, usuario.ContrasenaHash, datos.Contrasena));
        datos.Email = correo;
        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsJsonAsync("/api/autenticacion/registro", datos, Ct)).StatusCode);
        Assert.Equal(1, await db.Usuarios.CountAsync(x => x.Email == correo, Ct));
        Assert.Equal(1, await db.Clientes.CountAsync(x => x.IdUsuario == usuario.IdUsuario, Ct));
    }

    [Fact]
    public async Task Registro_revierte_usuario_y_perfil_si_falla_antes_del_commit()
    {
        var datos = Datos();
        using var conFallo = app.WithWebHostBuilder(b => b.ConfigureServices(s =>
            s.AddDbContext<BanosQuimicosDbContext>(o => o.AddInterceptors(new FalloAntesDelCommit()))));
        using var http = conFallo.CreateClient();
        var respuesta = await http.PostAsJsonAsync("/api/autenticacion/registro", datos, Ct);
        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        using var db = app.NuevoContexto();
        Assert.False(await db.Usuarios.AnyAsync(x => x.Email == datos.Email, Ct));
        Assert.False(await db.Clientes.AnyAsync(x => x.Usuario.Email == datos.Email, Ct));
    }

    private sealed class FalloAntesDelCommit : SaveChangesInterceptor
    {
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData datos, int resultado,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("PRUEBA: fallo después de guardar y antes del commit explícito.");
    }

    [Fact]
    public async Task Registro_concurrente_del_mismo_correo_no_deja_perfiles_huerfanos()
    {
        var datos = Datos();
        using var a = Cliente(); using var b = Cliente();
        var respuestas = await Task.WhenAll(a.PostAsJsonAsync("/api/autenticacion/registro", datos, Ct),
            b.PostAsJsonAsync("/api/autenticacion/registro", datos, Ct));
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        using var db = app.NuevoContexto();
        var usuario = await db.Usuarios.SingleAsync(x => x.Email == datos.Email, Ct);
        Assert.Equal(1, await db.Clientes.CountAsync(x => x.IdUsuario == usuario.IdUsuario, Ct));
    }

    [Fact]
    public async Task Registro_rechaza_campos_faltantes_y_escalamiento_de_privilegios()
    {
        using var http = Cliente();
        var datos = Datos();
        var json = JsonSerializer.SerializeToNode(datos, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();
        json["idRol"] = 1; json["estadoCuenta"] = "Activa";
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/autenticacion/registro", json, Ct)).StatusCode);
        datos.Telefono = "   ";
        Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync("/api/autenticacion/registro", datos, Ct)).StatusCode);
        using var db = app.NuevoContexto();
        Assert.False(await db.Usuarios.AnyAsync(x => x.Email == datos.Email, Ct));
    }

    [Fact]
    public async Task Login_rechaza_clave_incorrecta_y_cuenta_inexistente()
    {
        var (datos, _) = await Registrar();
        using var http = Cliente();
        var r = await http.PostAsJsonAsync("/api/autenticacion/login", new { datos.Email, contrasena = Guid.NewGuid().ToString() }, Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        var inexistente = await http.PostAsJsonAsync("/api/autenticacion/login", new { email = Datos().Email, contrasena = datos.Contrasena }, Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, inexistente.StatusCode);
        Assert.Equal(await r.Content.ReadAsStringAsync(Ct), await inexistente.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Pendiente_solo_obtiene_aviso_y_no_puede_activarse_ni_operar()
    {
        var (datos, id) = await Registrar();
        var token = await Token("  " + datos.Email.ToUpperInvariant() + "  ", datos.Contrasena);
        using var http = Cliente(token);
        var yo = await http.GetFromJsonAsync<JsonElement>("/api/autenticacion/yo", Ct);
        Assert.Equal(2, yo.EnumerateObject().Count());
        Assert.Equal("Pendiente", yo.GetProperty("estadoCuenta").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/_pruebas/operacion", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/_pruebas/sin-atributo", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsync($"/api/cuentas/{id}/activar", null, Ct)).StatusCode);
    }

    [Fact]
    public async Task Administrador_activa_y_cliente_no_puede_activar_otras_cuentas()
    {
        var (datos, id) = await Registrar();
        var tokenPrevio = await Token(datos.Email, datos.Contrasena);
        using var admin = Cliente(await TokenAdmin());
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/cuentas/{id}/activar", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/cuentas/{id}/activar", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync("/api/cuentas/2147483647/activar", null, Ct)).StatusCode);
        using var cliente = Cliente(tokenPrevio);
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.GetAsync("/_pruebas/operacion", Ct)).StatusCode);
        var yo = await cliente.GetFromJsonAsync<JsonElement>("/api/autenticacion/yo", Ct);
        Assert.Equal("Activa", yo.GetProperty("estadoCuenta").GetString());
        Assert.False(yo.TryGetProperty("contrasenaHash", out _));
        Assert.False(yo.TryGetProperty("contrasena", out _));
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsync($"/api/cuentas/{id}/activar", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/_pruebas/empleado", Ct)).StatusCode);
    }

    [Theory]
    [InlineData("Suspendida")]
    [InlineData("BajaLogica")]
    public async Task Cambiar_estado_invalida_el_acceso_de_una_sesion_anterior(string estado)
    {
        var (datos, id) = await Registrar();
        using (var admin = Cliente(await TokenAdmin()))
            Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/cuentas/{id}/activar", null, Ct)).StatusCode);
        var token = await Token(datos.Email, datos.Contrasena);
        using var http = Cliente(token);
        Assert.Equal(HttpStatusCode.NoContent, (await http.GetAsync("/_pruebas/operacion", Ct)).StatusCode);
        using (var db = app.NuevoContexto())
            await db.Usuarios.Where(x => x.IdUsuario == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.EstadoCuenta, estado), Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/_pruebas/operacion", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/autenticacion/yo", Ct)).StatusCode);
        using var anonimo = Cliente();
        Assert.Equal(HttpStatusCode.Forbidden, (await anonimo.PostAsJsonAsync("/api/autenticacion/login", new { datos.Email, datos.Contrasena }, Ct)).StatusCode);
    }

    [Fact]
    public async Task Cambiar_rol_se_refleja_en_el_token_previo_sin_reiniciar_sesion()
    {
        var (datos, id) = await Registrar();
        using (var admin = Cliente(await TokenAdmin()))
            await admin.PostAsync($"/api/cuentas/{id}/activar", null, Ct);
        using var http = Cliente(await Token(datos.Email, datos.Contrasena));
        using (var db = app.NuevoContexto())
        {
            var rol = await db.Roles.SingleAsync(x => x.NombreRol == "Empleado", Ct);
            await db.Usuarios.Where(x => x.IdUsuario == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.IdRol, rol.IdRol), Ct);
        }
        Assert.Equal(HttpStatusCode.NoContent, (await http.GetAsync("/_pruebas/empleado", Ct)).StatusCode);
        using (var db = app.NuevoContexto())
        {
            var rol = await db.Roles.SingleAsync(x => x.NombreRol == "Cliente", Ct);
            await db.Usuarios.Where(x => x.IdUsuario == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.IdRol, rol.IdRol), Ct);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/_pruebas/empleado", Ct)).StatusCode);
    }

    [Fact]
    public async Task Rechaza_anonimos_tokens_alterados_vencidos_y_con_audiencia_incorrecta()
    {
        using var anonimo = Cliente();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/api/autenticacion/yo", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsync("/api/cuentas/1/activar", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonimo.GetAsync("/api/estado", Ct)).StatusCode);
        var (datos, id) = await Registrar();
        var original = await Token(datos.Email, datos.Contrasena);
        var partes = original.Split('.');
        partes[2] = (partes[2][0] == 'A' ? "B" : "A") + partes[2][1..];
        using var alterado = Cliente(string.Join('.', partes));
        Assert.Equal(HttpStatusCode.Unauthorized, (await alterado.GetAsync("/api/autenticacion/yo", Ct)).StatusCode);
        foreach (var caso in new[] { "vencido", "audiencia", "emisor" })
        {
            var token = new JwtSecurityToken(caso == "emisor" ? "Otro" : "BanosQuimicos.Api",
                caso == "audiencia" ? "Otra" : "BanosQuimicos.App",
                [new Claim(JwtRegisteredClaimNames.Sub, id.ToString())],
                DateTime.UtcNow.AddMinutes(-10), caso == "vencido" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5),
                new SigningCredentials(new SymmetricSecurityKey(Convert.FromBase64String(app.ClaveJwt)), SecurityAlgorithms.HmacSha256));
            using var invalido = Cliente(new JwtSecurityTokenHandler().WriteToken(token));
            Assert.Equal(HttpStatusCode.Unauthorized, (await invalido.GetAsync("/api/autenticacion/yo", Ct)).StatusCode);
        }
    }

    [Fact]
    public async Task Bootstrap_no_crea_otro_administrador_ni_modifica_cuentas()
    {
        using var db = app.NuevoContexto();
        var antes = await db.Usuarios.CountAsync(Ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new PrimerAdministrador(db, new PasswordHasher<Usuario>()).CrearAsync(Datos(), Ct));
        Assert.Equal(antes, await db.Usuarios.CountAsync(Ct));
    }
}
