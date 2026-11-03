using System.Net;
using System.Net.Http.Json;
using BanosQuimicos.App.Autenticacion;
using Xunit;

namespace BanosQuimicos.Api.Pruebas;

public class AutenticacionAppPruebas
{
    private static CuentaActual Activa => new() { IdUsuario = 50, Nombre = "PRUEBA", Rol = "Cliente", EstadoCuenta = "Activa" };
    private static HttpResponseMessage Json(object datos) => new(HttpStatusCode.OK) { Content = JsonContent.Create(datos) };
    private static DatosLogin Login => new() { Email = "prueba@example.invalid", Contrasena = Guid.NewGuid().ToString() };
    private static HttpResponseMessage Token => Json(new RespuestaLogin(Guid.NewGuid().ToString(), DateTimeOffset.UtcNow.AddMinutes(30), "Activa", null));

    private sealed class Transporte(Func<HttpRequestMessage, Task<HttpResponseMessage>> enviar) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => enviar(request);
    }
    private static ClienteAutenticacion Cliente(Func<HttpRequestMessage, Task<HttpResponseMessage>> enviar) =>
        new(new HttpClient(new Transporte(enviar)) { BaseAddress = new Uri("https://api.example.invalid/") });
    internal class AlmacenPrueba : IAlmacenSesion
    {
        public string? Token;
        public bool FallarLectura, FallarEscritura;
        public Task<string?> LeerAsync() => FallarLectura ? throw new InvalidOperationException("PRUEBA") : Task.FromResult(Token);
        public Task GuardarAsync(string token) { if (FallarEscritura) throw new InvalidOperationException("PRUEBA"); Token = token; return Task.CompletedTask; }
        public Task BorrarAsync() { Token = null; return Task.CompletedTask; }
    }

    [Fact]
    public async Task Cliente_no_envia_token_en_login_registro_y_lo_limita_a_yo()
    {
        var rutas = new List<string>();
        var api = Cliente(request =>
        {
            Assert.Equal("api.example.invalid", request.RequestUri!.Host);
            rutas.Add(request.RequestUri.AbsolutePath);
            if (request.RequestUri.AbsolutePath.EndsWith("yo"))
            { Assert.Equal("PRUEBA", request.Headers.Authorization!.Parameter); return Task.FromResult(Json(Activa)); }
            Assert.Null(request.Headers.Authorization);
            return Task.FromResult(request.RequestUri.AbsolutePath.EndsWith("login") ? Token : new HttpResponseMessage(HttpStatusCode.Created));
        });
        await api.RegistrarAsync(new DatosRegistro());
        await api.LoginAsync(Login);
        await api.ConsultarAsync("PRUEBA");
        Assert.Equal(["/api/autenticacion/registro", "/api/autenticacion/login", "/api/autenticacion/yo"], rutas);
    }

    [Fact]
    public async Task Sesion_no_confia_en_estado_del_login_y_descarta_perfil_de_pendiente()
    {
        var almacen = new AlmacenPrueba();
        var api = Cliente(r => Task.FromResult(r.RequestUri!.AbsolutePath.EndsWith("login") ? Token :
            Json(new CuentaActual { EstadoCuenta = "Pendiente", Nombre = "NO MOSTRAR", Rol = "Administrador" })));
        var sesion = new SesionUsuario(api, almacen);
        await sesion.InicializarAsync();
        await sesion.IngresarAsync(Login);
        Assert.Equal(EstadoSesion.Pendiente, sesion.Estado);
        Assert.Null(sesion.Cuenta!.Nombre); Assert.Null(sesion.Cuenta.Rol);
        Assert.NotNull(almacen.Token);
    }

    [Fact]
    public async Task Restaurar_siempre_consulta_yo_y_cerrar_borra_la_sesion()
    {
        var almacen = new AlmacenPrueba { Token = "PRUEBA" };
        var consultas = 0;
        var sesion = new SesionUsuario(Cliente(_ => { consultas++; return Task.FromResult(Json(Activa)); }), almacen);
        await sesion.InicializarAsync();
        Assert.Equal(1, consultas); Assert.Equal(EstadoSesion.Activa, sesion.Estado);
        await sesion.CerrarAsync();
        Assert.Null(almacen.Token); Assert.Null(sesion.Cuenta); Assert.Equal(EstadoSesion.SinSesion, sesion.Estado);
        await new SesionUsuario(Cliente(_ => throw new Exception("No debe llamar sin token")), almacen).InicializarAsync();
    }

    [Fact]
    public async Task Perdida_de_conexion_oculta_la_cuenta_y_permite_reintentar()
    {
        var fallar = false;
        var api = Cliente(_ => fallar ? throw new HttpRequestException() : Task.FromResult(Json(Activa)));
        var almacen = new AlmacenPrueba { Token = "PRUEBA" };
        var sesion = new SesionUsuario(api, almacen);
        await sesion.InicializarAsync();
        fallar = true; await sesion.ActualizarAsync();
        Assert.Equal(EstadoSesion.SinVerificar, sesion.Estado); Assert.Null(sesion.Cuenta);
        Assert.Equal("PRUEBA", almacen.Token);
        fallar = false; await sesion.ActualizarAsync();
        Assert.Equal(EstadoSesion.Activa, sesion.Estado);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, EstadoSesion.SinSesion, true)]
    [InlineData(HttpStatusCode.Forbidden, EstadoSesion.SinVerificar, false)]
    public async Task Distingue_401_de_403_al_revisar_sesion(HttpStatusCode codigo, EstadoSesion esperado, bool borrar)
    {
        var almacen = new AlmacenPrueba { Token = "PRUEBA" };
        var sesion = new SesionUsuario(Cliente(_ => Task.FromResult(new HttpResponseMessage(codigo))), almacen);
        await sesion.InicializarAsync();
        Assert.Equal(esperado, sesion.Estado); Assert.Null(sesion.Cuenta);
        Assert.Equal(borrar, almacen.Token is null);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, TipoErrorApi.Credenciales)]
    [InlineData(HttpStatusCode.Forbidden, TipoErrorApi.CuentaNoHabilitada)]
    [InlineData(HttpStatusCode.BadRequest, TipoErrorApi.Validacion)]
    [InlineData(HttpStatusCode.Conflict, TipoErrorApi.Conflicto)]
    [InlineData(HttpStatusCode.InternalServerError, TipoErrorApi.Servidor)]
    [InlineData(HttpStatusCode.Redirect, TipoErrorApi.Servidor)]
    public async Task Login_traduce_errores_sin_exponer_el_cuerpo(HttpStatusCode codigo, TipoErrorApi esperado)
    {
        var api = Cliente(_ => Task.FromResult(new HttpResponseMessage(codigo) { Content = new StringContent("NO MOSTRAR RESPUESTA INTERNA") }));
        var error = await Assert.ThrowsAsync<ErrorApi>(() => api.LoginAsync(Login));
        Assert.Equal(esperado, error.Tipo); Assert.DoesNotContain("NO MOSTRAR", error.Message);
    }

    [Fact]
    public async Task No_duplica_peticion_si_se_consulta_mientras_esta_en_curso()
    {
        var completar = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var llamadas = 0;
        var sesion = new SesionUsuario(Cliente(_ => { llamadas++; return completar.Task; }), new AlmacenPrueba { Token = "PRUEBA" });
        var primera = sesion.InicializarAsync();
        await sesion.ActualizarAsync();
        Assert.Equal(1, llamadas);
        completar.SetResult(Json(Activa));
        await primera;
        Assert.False(sesion.Ocupada);
    }

    [Fact]
    public async Task Almacen_no_disponible_no_impide_sesion_en_memoria()
    {
        var almacen = new AlmacenPrueba { FallarEscritura = true };
        var sesion = new SesionUsuario(Cliente(r => Task.FromResult(r.RequestUri!.AbsolutePath.EndsWith("login") ? Token : Json(Activa))), almacen);
        await sesion.InicializarAsync(); await sesion.IngresarAsync(Login);
        Assert.Equal(EstadoSesion.Activa, sesion.Estado); Assert.Null(almacen.Token);
        Assert.Contains("solo mientras", sesion.Mensaje);
    }

    [Fact]
    public async Task Almacen_ilegible_se_limpia_y_solicita_login()
    {
        var almacen = new AlmacenPrueba { FallarLectura = true, Token = "PRUEBA" };
        var sesion = new SesionUsuario(Cliente(_ => throw new Exception("No debe llamar con token ilegible")), almacen);
        await sesion.InicializarAsync();
        Assert.Equal(EstadoSesion.SinSesion, sesion.Estado); Assert.Null(almacen.Token);
    }

    [Fact]
    public async Task Respuesta_malformada_no_habilita_cuenta()
    {
        var api = Cliente(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>error</html>") }));
        var sesion = new SesionUsuario(api, new AlmacenPrueba { Token = "PRUEBA" });
        await sesion.InicializarAsync();
        Assert.Equal(EstadoSesion.SinVerificar, sesion.Estado); Assert.Null(sesion.Cuenta);
    }
}
