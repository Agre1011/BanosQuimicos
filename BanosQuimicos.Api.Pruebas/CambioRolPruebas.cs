using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using BanosQuimicos.Api.Contratos.Autenticacion;
using BanosQuimicos.Api.Contratos.Cuentas;
using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AutenticacionApp = global::BanosQuimicos.App.Autenticacion;

namespace BanosQuimicos.Api.Pruebas;

public class CambioRolPruebas(AplicacionPruebas app, ITestOutputHelper salida) : IClassFixture<AplicacionPruebas>
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private HttpClient Http(string? token = null)
    {
        var http = app.CreateClient();
        if (token is not null) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }
    private async Task<string> Token(string correo, string clave)
    {
        using var http = Http();
        var r = await http.PostAsJsonAsync("/api/autenticacion/login", new { email = correo, contrasena = clave }, Ct);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<LoginRespuesta>(Ct))!.AccessToken;
    }
    private async Task<(RegistroClienteSolicitud Datos, int Id)> Registrar()
    {
        var datos = new RegistroClienteSolicitud {
            Nombre = "PRUEBA ROL", Apellido = Guid.NewGuid().ToString("N"),
            Email = "prueba-rol-" + Guid.NewGuid().ToString("N") + "@example.invalid",
            Telefono = "+00 PRUEBA", Contrasena = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)),
            RazonSocial = "PRUEBA PERFIL", Cuit = "PRUEBA", DireccionPrincipal = "PRUEBA LOCAL"
        };
        using var http = Http();
        Assert.Equal(HttpStatusCode.Created, (await http.PostAsJsonAsync("/api/autenticacion/registro", datos, Ct)).StatusCode);
        using var db = app.NuevoContexto();
        return (datos, await db.Usuarios.Where(x => x.Email == datos.Email).Select(x => x.IdUsuario).SingleAsync(Ct));
    }
    private async Task<HttpClient> Admin() => Http(await Token(app.CorreoAdmin, app.ClaveAdmin));
    private Task<HttpResponseMessage> Cambiar(HttpClient http, int id, string anterior, string nuevo) =>
        http.PutAsJsonAsync($"/api/cuentas/{id}/rol", new { rolAnterior = anterior, nuevoRol = nuevo }, Ct);

    [Fact]
    public async Task Registro_activo_cambio_login_misma_clave_vuelta_e_historial_conservado()
    {
        salida.WriteLine("Base aislada conservada: " + app.NombreBase);
        var (datos, id) = await Registrar();
        using var db = app.NuevoContexto();
        var original = await db.Usuarios.AsNoTracking().SingleAsync(x => x.IdUsuario == id, Ct);
        var perfil = await db.Clientes.AsNoTracking().SingleAsync(x => x.IdUsuario == id, Ct);
        using var http = Http();
        var api = new AutenticacionApp.ClienteAutenticacion(http);
        var sesion = new AutenticacionApp.SesionUsuario(api, new AutenticacionAppPruebas.AlmacenPrueba());
        await sesion.InicializarAsync();
        await sesion.IngresarAsync(new AutenticacionApp.DatosLogin { Email = datos.Email, Contrasena = datos.Contrasena });
        Assert.Equal(AutenticacionApp.EstadoSesion.Activa, sesion.Estado);
        using var admin = await Admin();
        using var appAdminHttp = Http();
        var appAdmin = new AutenticacionApp.SesionUsuario(new AutenticacionApp.ClienteAutenticacion(appAdminHttp), new AutenticacionAppPruebas.AlmacenPrueba());
        await appAdmin.InicializarAsync();
        await appAdmin.IngresarAsync(new AutenticacionApp.DatosLogin { Email = app.CorreoAdmin, Contrasena = app.ClaveAdmin });
        await appAdmin.CambiarRolAsync(id, "Cliente", "Empleado");
        await sesion.CerrarAsync();
        await sesion.IngresarAsync(new AutenticacionApp.DatosLogin { Email = datos.Email, Contrasena = datos.Contrasena });
        Assert.Equal(AutenticacionApp.EstadoSesion.Activa, sesion.Estado); Assert.Equal("Empleado", sesion.Cuenta!.Rol);
        Assert.Equal(id, sesion.Cuenta.IdUsuario);
        await Assert.ThrowsAsync<AutenticacionApp.ErrorApi>(() => sesion.ListarCuentasAsync(1, null, null));
        var listado = await appAdmin.ListarCuentasAsync(1, datos.Email, null, "Empleado");
        Assert.Equal(id, Assert.Single(listado.Elementos).IdUsuario);
        // Historial exclusivamente de la base de prueba: cambiar el rol no borra al responsable.
        var bano = new BanoQuimico { CodigoInterno = "PRUEBA-" + Guid.NewGuid().ToString("N")[..8], IdTipoBano = 1, IdEstadoBano = 1, FechaAlta = DateTimeOffset.UtcNow };
        var tarea = new TareaOperativa { Bano = bano, IdEmpleado = id, IdTipoOperacion = 11, IdEstadoOperacion = 8, FechaRegistro = DateTimeOffset.UtcNow, FechaRealizada = DateTimeOffset.UtcNow, ObservacionesEmpleado = "PRUEBA HISTORIAL" };
        db.TareasOperativas.Add(tarea); await db.SaveChangesAsync(Ct);
        await appAdmin.CambiarRolAsync(id, "Empleado", "Cliente");
        var actual = await db.Usuarios.AsNoTracking().SingleAsync(x => x.IdUsuario == id, Ct);
        var perfilActual = await db.Clientes.AsNoTracking().SingleAsync(x => x.IdUsuario == id, Ct);
        Assert.True(original.ContrasenaHash == actual.ContrasenaHash, "El hash no debe cambiar al cambiar el rol.");
        Assert.Equal(original.FechaRegistro, actual.FechaRegistro); Assert.Equal(original.Email, actual.Email);
        Assert.Equal(perfil.IdCliente, perfilActual.IdCliente); Assert.Equal(perfil.RazonSocial, perfilActual.RazonSocial);
        Assert.Equal(perfil.Cuit, perfilActual.Cuit); Assert.Equal(perfil.DireccionPrincipal, perfilActual.DireccionPrincipal);
        Assert.Equal(id, (await db.TareasOperativas.AsNoTracking().SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct)).IdEmpleado);
        Assert.Equal(1, await db.Clientes.CountAsync(x => x.IdUsuario == id, Ct));
        await sesion.ActualizarAsync();
        Assert.Equal("Cliente", sesion.Cuenta!.Rol);
        var detalle = await appAdmin.DetalleCuentaAsync(id);
        Assert.Equal("Activa", detalle.EstadoCuenta);
        Assert.Empty((await appAdmin.ListarCuentasAsync(1, datos.Email, null, "Empleado")).Elementos);
    }

    // Renderiza los Razor reales y ejecuta sus callbacks en el dispatcher de Blazor.
    // No es automatización del teléfono: comprueba el hueco que los tests HTTP no cubrían.
    private sealed class CapturadorComponentes : Microsoft.AspNetCore.Components.IComponentActivator
    {
        public readonly Dictionary<Type, Microsoft.AspNetCore.Components.IComponent> Instancias = [];
        public Microsoft.AspNetCore.Components.IComponent CreateInstance(Type tipo)
        {
            var instancia = (Microsoft.AspNetCore.Components.IComponent)Activator.CreateInstance(tipo)!;
            Instancias[tipo] = instancia;
            return instancia;
        }
    }
    private static async Task Callback(object componente, string nombre, params object?[] args)
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var resultado = componente.GetType().GetMethod(nombre, flags)!.Invoke(componente, args);
        if (resultado is Task tarea) await tarea;
        typeof(Microsoft.AspNetCore.Components.ComponentBase).GetMethod("StateHasChanged", flags)!.Invoke(componente, null);
    }

    [Fact]
    public async Task Seleccionar_no_guarda_confirmar_persiste_y_login_abre_inicio_empleado()
    {
        var (datos, id) = await Registrar();
        using var admin = await Admin();
        using var http = Http();
        var api = new AutenticacionApp.ClienteAutenticacion(http);
        var sesion = new AutenticacionApp.SesionUsuario(api, new AutenticacionAppPruebas.AlmacenPrueba());
        await sesion.InicializarAsync();
        await sesion.IngresarAsync(new AutenticacionApp.DatosLogin { Email = app.CorreoAdmin, Contrasena = app.ClaveAdmin });
        var activador = new CapturadorComponentes();
        var servicios = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(servicios, api);
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(servicios, sesion);
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<Microsoft.AspNetCore.Components.IComponentActivator>(servicios, activador);
        Microsoft.Extensions.DependencyInjection.LoggingServiceCollectionExtensions.AddLogging(servicios);
        await using var proveedor = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(servicios);
        await using var renderer = new Microsoft.AspNetCore.Components.Web.HtmlRenderer(proveedor,
            Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>(proveedor));
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var vista = await renderer.RenderComponentAsync<BanosQuimicos.App.Components.Cuentas.GestionCuentas>();
            var componente = activador.Instancias[typeof(BanosQuimicos.App.Components.Cuentas.GestionCuentas)];
            componente.GetType().GetField("busqueda", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(componente, datos.Email);
            await Callback(componente, "BuscarAsync");
            var cuenta = await sesion.DetalleCuentaAsync(id);
            await Callback(componente, "PrepararRol", cuenta, "Empleado");
            var pendiente = vista.ToHtmlString();
            Assert.Contains("Cambio sin guardar:", pendiente);
            Assert.Contains("Rol guardado: <strong>Cliente</strong>", pendiente);
            Assert.DoesNotContain("Rol guardado: <strong>Empleado</strong>", pendiente);
            using var db = app.NuevoContexto();
            Assert.Equal("Cliente", await db.Usuarios.Where(x => x.IdUsuario == id).Select(x => x.Rol.NombreRol).SingleAsync(Ct));
            using var persona = Http(await Token(datos.Email, datos.Contrasena));
            Assert.Equal("Cliente", (await persona.GetFromJsonAsync<UsuarioRespuesta>("/api/autenticacion/yo", Ct))!.Rol);
            // Selección sin confirmar reproduce el resultado comunicado por el estudiante.
            await Callback(componente, "CancelarRol");
            Assert.DoesNotContain("Cambio sin guardar:", vista.ToHtmlString());
            await Callback(componente, "PrepararRol", cuenta, "Empleado");
            await Callback(componente, "ConfirmarRolAsync");
            Assert.Contains("Rol guardado: Empleado", vista.ToHtmlString());
            Assert.Equal("Empleado", await db.Usuarios.Where(x => x.IdUsuario == id).Select(x => x.Rol.NombreRol).SingleAsync(Ct));
            await sesion.CerrarAsync();
            // Nueva sesión: mismo correo y contraseña, token nuevo, mismo IdUsuario.
            var login = await api.LoginAsync(new AutenticacionApp.DatosLogin { Email = datos.Email, Contrasena = datos.Contrasena });
            var sub = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken).Subject;
            Assert.Equal(id.ToString(), sub); // El login no tiene campo rol: solo identidad en el token.
            Assert.Equal("Empleado", (await api.ConsultarAsync(login.AccessToken)).Rol);
            await sesion.IngresarAsync(new AutenticacionApp.DatosLogin { Email = datos.Email, Contrasena = datos.Contrasena });
            Assert.Equal(id, sesion.Cuenta!.IdUsuario); Assert.Equal("Empleado", sesion.Cuenta.Rol);
            var inicio = await renderer.RenderComponentAsync<BanosQuimicos.App.Components.Pages.Home>();
            var html = inicio.ToHtmlString();
            Assert.Contains("Rol: Empleado", html); Assert.Contains("Tareas asignadas", html);
            Assert.DoesNotContain("Gestionar cuentas", html);
            salida.WriteLine($"IdUsuario={id}: selección sin guardar=Cliente; guardado confirmado=Empleado; login nuevo sub={sub}; /yo=Empleado; sesión=Empleado; Home=InicioEmpleado.");
        });
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("Activa")]
    [InlineData("Suspendida")]
    [InlineData("BajaLogica")]
    public async Task Cambiar_rol_no_cambia_estado_y_pendiente_sigue_restringido(string estado)
    {
        var (datos, id) = await Registrar();
        using var db = app.NuevoContexto();
        await db.Usuarios.Where(x => x.IdUsuario == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.EstadoCuenta, estado), Ct);
        using var admin = await Admin();
        Assert.Equal(HttpStatusCode.NoContent, (await Cambiar(admin, id, "Cliente", "Empleado")).StatusCode);
        Assert.Equal(estado, await db.Usuarios.Where(x => x.IdUsuario == id).Select(x => x.EstadoCuenta).SingleAsync(Ct));
        if (estado == "Pendiente")
        {
            using var usuario = Http(await Token(datos.Email, datos.Contrasena));
            var yo = await usuario.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/autenticacion/yo", Ct);
            Assert.Equal("Pendiente", yo.GetProperty("estadoCuenta").GetString());
            Assert.False(yo.TryGetProperty("rol", out _));
            Assert.Equal(HttpStatusCode.Forbidden, (await usuario.GetAsync("/api/cuentas", Ct)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await usuario.GetAsync("/_pruebas/operacion", Ct)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NoContent, (await Cambiar(admin, id, "Empleado", "Cliente")).StatusCode);
        Assert.Equal(estado, await db.Usuarios.Where(x => x.IdUsuario == id).Select(x => x.EstadoCuenta).SingleAsync(Ct));
    }

    [Theory]
    [InlineData("Anonimo", 401)]
    [InlineData("Pendiente", 403)]
    [InlineData("Cliente", 403)]
    [InlineData("Empleado", 403)]
    [InlineData("Suspendida", 401)]
    [InlineData("BajaLogica", 401)]
    public async Task Solo_administrador_activo_puede_cambiar_roles_y_gestionar(string caso, int codigo)
    {
        var (_, objetivo) = await Registrar();
        var (datos, actor) = await Registrar();
        using var admin = await Admin();
        if (caso == "Pendiente") { using var pendiente = app.NuevoContexto(); await pendiente.Usuarios.Where(x => x.IdUsuario == actor).ExecuteUpdateAsync(x => x.SetProperty(u => u.EstadoCuenta, "Pendiente"), Ct); }
        if (caso == "Empleado") await Cambiar(admin, actor, "Cliente", "Empleado");
        string? token = caso == "Anonimo" ? null : await Token(datos.Email, datos.Contrasena);
        if (caso is "Suspendida" or "BajaLogica")
        {
            using var db = app.NuevoContexto();
            var rolAdmin = await db.Roles.Where(x => x.NombreRol == "Administrador").Select(x => x.IdRol).SingleAsync(Ct);
            await db.Usuarios.Where(x => x.IdUsuario == actor).ExecuteUpdateAsync(s => s.SetProperty(x => x.IdRol, rolAdmin), Ct);
            token = await Token(datos.Email, datos.Contrasena);
            await db.Usuarios.Where(x => x.IdUsuario == actor).ExecuteUpdateAsync(s => s.SetProperty(x => x.EstadoCuenta, caso), Ct);
        }
        using var usuario = Http(token);
        Assert.Equal((HttpStatusCode)codigo, (await Cambiar(usuario, objetivo, "Cliente", "Empleado")).StatusCode);
        Assert.Equal((HttpStatusCode)codigo, (await usuario.GetAsync("/api/cuentas", Ct)).StatusCode);
        Assert.Equal((HttpStatusCode)codigo, (await usuario.GetAsync($"/api/cuentas/{objetivo}", Ct)).StatusCode);
        using var verificar = app.NuevoContexto();
        Assert.Equal("Cliente", await verificar.Usuarios.Where(x => x.IdUsuario == objetivo).Select(x => x.Rol.NombreRol).SingleAsync(Ct));
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Otro")]
    [InlineData("")]
    public async Task No_admite_elegir_administrador_ni_roles_inventados(string rol)
    {
        var (_, id) = await Registrar();
        using var admin = await Admin();
        Assert.Equal(HttpStatusCode.BadRequest, (await Cambiar(admin, id, "Cliente", rol)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync("/api/cuentas?rol=Administrador", Ct)).StatusCode);
    }

    [Fact]
    public async Task Protege_administradores_no_admite_estado_y_detecta_repeticiones()
    {
        var (_, id) = await Registrar();
        using var admin = await Admin();
        using var db = app.NuevoContexto();
        var idAdmin = await db.Usuarios.Where(x => x.Email == app.CorreoAdmin).Select(x => x.IdUsuario).SingleAsync(Ct);
        Assert.Equal(HttpStatusCode.Conflict, (await Cambiar(admin, idAdmin, "Cliente", "Empleado")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"/api/cuentas/{id}/rol", new { rolAnterior = "Cliente", nuevoRol = "Empleado", estadoCuenta = "Activa" }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Cambiar(admin, id, "Cliente", "Cliente")).StatusCode);
        var resultados = await Task.WhenAll(Cambiar(admin, id, "Cliente", "Empleado"), Cambiar(admin, id, "Cliente", "Empleado"));
        Assert.Single(resultados, x => x.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(resultados, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.Conflict, (await Cambiar(admin, id, "Cliente", "Empleado")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Cambiar(admin, int.MaxValue, "Cliente", "Empleado")).StatusCode);
        Assert.Equal("Activa", await db.Usuarios.Where(x => x.IdUsuario == id).Select(x => x.EstadoCuenta).SingleAsync(Ct));
    }
}
