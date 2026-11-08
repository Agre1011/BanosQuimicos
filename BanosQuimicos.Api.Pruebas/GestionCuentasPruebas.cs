using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using BanosQuimicos.Api.Contratos.Autenticacion;
using BanosQuimicos.Api.Contratos.Cuentas;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanosQuimicos.Api.Pruebas;

public class GestionCuentasPruebas(AplicacionPruebas app, ITestOutputHelper salida) : IClassFixture<AplicacionPruebas>
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
    private async Task<(int Id, string Token)> Alta(string nombre, string apellido, string estado = "Pendiente", string rol = "Cliente")
    {
        var email = "prueba-cuentas-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var clave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        using var http = Http();
        var r = await http.PostAsJsonAsync("/api/autenticacion/registro", new RegistroClienteSolicitud {
            Nombre = nombre, Apellido = apellido, Email = email, Telefono = "+00 PRUEBA CUENTAS", Contrasena = clave
        }, Ct);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        using var db = app.NuevoContexto();
        var usuario = await db.Usuarios.SingleAsync(x => x.Email == email, Ct);
        usuario.EstadoCuenta = estado;
        usuario.IdRol = await db.Roles.Where(x => x.NombreRol == rol).Select(x => x.IdRol).SingleAsync(Ct);
        await db.SaveChangesAsync(Ct);
        return (usuario.IdUsuario, await Token(email, clave));
    }
    private Task<string> Admin() => Token(app.CorreoAdmin, app.ClaveAdmin);

    [Theory]
    [InlineData("Anonimo", 401)]
    [InlineData("Pendiente", 403)]
    [InlineData("Cliente", 403)]
    [InlineData("Empleado", 403)]
    [InlineData("Suspendida", 401)]
    [InlineData("BajaLogica", 401)]
    public async Task Todas_las_operaciones_exigen_administrador_actual_activo(string caso, int codigo)
    {
        var objetivo = await Alta("PRUEBA CUENTAS", Guid.NewGuid().ToString("N"));
        string? token = null;
        if (caso != "Anonimo")
        {
            var actor = await Alta("PRUEBA CUENTAS ACTOR", Guid.NewGuid().ToString("N"),
                caso == "Pendiente" ? "Pendiente" : "Activa",
                caso is "Suspendida" or "BajaLogica" ? "Administrador" : caso == "Empleado" ? "Empleado" : "Cliente");
            token = actor.Token;
            if (caso is "Suspendida" or "BajaLogica")
            {
                using var db = app.NuevoContexto();
                await db.Usuarios.Where(x => x.IdUsuario == actor.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.EstadoCuenta, caso), Ct);
            }
        }
        using var http = Http(token);
        Assert.Equal((HttpStatusCode)codigo, (await http.GetAsync("/api/cuentas", Ct)).StatusCode);
        Assert.Equal((HttpStatusCode)codigo, (await http.GetAsync($"/api/cuentas/{objetivo.Id}", Ct)).StatusCode);
        Assert.Equal((HttpStatusCode)codigo, (await http.PostAsync($"/api/cuentas/{objetivo.Id}/activar", null, Ct)).StatusCode);
        using var verificar = app.NuevoContexto();
        Assert.Equal("Pendiente", await verificar.Usuarios.Where(x => x.IdUsuario == objetivo.Id).Select(x => x.EstadoCuenta).SingleAsync(Ct));
    }

    [Fact]
    public async Task Pagina_tiene_orden_estable_limite_y_no_expone_secretos()
    {
        salida.WriteLine("Base aislada conservada: " + app.NombreBase);
        var marca = "PAGINA" + Guid.NewGuid().ToString("N");
        var ids = new List<int>();
        for (var i = 0; i < 23; i++) ids.Add((await Alta("PRUEBA CUENTAS", marca)).Id);
        using var http = Http(await Admin());
        var recibidos = new List<int>();
        for (var pagina = 1; pagina <= 4; pagina++)
        {
            var ruta = $"/api/cuentas?busqueda={marca}&pagina={pagina}&tamanoPagina=7";
            var resultado = (await http.GetFromJsonAsync<PaginaCuentasRespuesta>(ruta, Ct))!;
            Assert.Equal(23, resultado.Total); Assert.Equal(pagina, resultado.Pagina);
            Assert.Equal(pagina == 4 ? 2 : 7, resultado.Elementos.Count);
            recibidos.AddRange(resultado.Elementos.Select(x => x.IdUsuario));
            var repetir = (await http.GetFromJsonAsync<PaginaCuentasRespuesta>(ruta, Ct))!;
            Assert.Equal(resultado.Elementos.Select(x => x.IdUsuario), repetir.Elementos.Select(x => x.IdUsuario));
        }
        Assert.Equal(ids.Order(), recibidos); Assert.Equal(23, recibidos.Distinct().Count());
        var vacia = (await http.GetFromJsonAsync<PaginaCuentasRespuesta>($"/api/cuentas?busqueda={marca}&pagina=99", Ct))!;
        Assert.Empty(vacia.Elementos); Assert.Equal(23, vacia.Total);
        var detalle = await http.GetFromJsonAsync<JsonElement>($"/api/cuentas/{ids[0]}", Ct);
        var permitidos = new[] { "idUsuario", "nombre", "apellido", "email", "telefono", "rol", "estadoCuenta", "fechaRegistro", "puedeActivar", "motivoSuspension", "fechaBaja" };
        Assert.Equal(permitidos.Order(), detalle.EnumerateObject().Select(x => x.Name).Order());
        var listaJson = await http.GetStringAsync($"/api/cuentas?busqueda={marca}", Ct);
        Assert.DoesNotContain("hash", listaJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", listaJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contrasena", listaJson, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("pagina=0")]
    [InlineData("tamanoPagina=0")]
    [InlineData("tamanoPagina=51")]
    [InlineData("pagina=2147483647&tamanoPagina=50")]
    [InlineData("estado=Inventado")]
    public async Task Rechaza_parametros_fuera_de_limite(string consulta)
    {
        using var http = Http(await Admin());
        Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/api/cuentas?" + consulta, Ct)).StatusCode);
    }

    [Fact]
    public async Task Busca_nombre_apellido_correo_combina_filtros_y_trata_porcentaje_literal()
    {
        var marca = "BUSCA" + Guid.NewGuid().ToString("N");
        var a = await Alta("PRUEBA " + marca, "CUENTAS");
        var b = await Alta("PRUEBA CUENTAS", marca, "Activa");
        var c = await Alta("PRUEBA % CUENTAS", marca, "Pendiente");
        using (var db = app.NuevoContexto())
            await db.Usuarios.Where(x => x.IdUsuario == c.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.EstadoCuenta, "Suspendida"), Ct);
        using var http = Http(await Admin());
        var todas = (await http.GetFromJsonAsync<PaginaCuentasRespuesta>($"/api/cuentas?busqueda={marca}", Ct))!;
        Assert.Equal(3, todas.Total);
        foreach (var (estado, id) in new[] { ("Pendiente", a.Id), ("Activa", b.Id), ("Suspendida", c.Id) })
        {
            var r = (await http.GetFromJsonAsync<PaginaCuentasRespuesta>($"/api/cuentas?busqueda={marca}&estado={estado}", Ct))!;
            Assert.Equal(id, Assert.Single(r.Elementos).IdUsuario);
        }
        using var contexto = app.NuevoContexto();
        var email = await contexto.Usuarios.Where(x => x.IdUsuario == a.Id).Select(x => x.Email).SingleAsync(Ct);
        var porCorreo = (await http.GetFromJsonAsync<PaginaCuentasRespuesta>("/api/cuentas?busqueda=" + Uri.EscapeDataString("  " + email.ToUpperInvariant() + "  "), Ct))!;
        Assert.Equal(a.Id, Assert.Single(porCorreo.Elementos).IdUsuario);
        var literal = (await http.GetFromJsonAsync<PaginaCuentasRespuesta>("/api/cuentas?busqueda=%25", Ct))!;
        Assert.All(literal.Elementos, x => Assert.Contains("%", x.Nombre + x.Apellido + x.Email));
        Assert.Contains(literal.Elementos, x => x.IdUsuario == c.Id);
        var vacia = (await http.GetFromJsonAsync<PaginaCuentasRespuesta>("/api/cuentas?busqueda=" + Guid.NewGuid(), Ct))!;
        Assert.Equal(0, vacia.Total); Assert.Empty(vacia.Elementos);
    }

    [Fact]
    public async Task Sesion_admin_real_consulta_activa_y_actualiza_contrato_de_app()
    {
        var objetivo = await Alta("PRUEBA CUENTAS APP", Guid.NewGuid().ToString("N"));
        using var http = Http();
        var api = new BanosQuimicos.App.Autenticacion.ClienteAutenticacion(http);
        var almacen = new AutenticacionAppPruebas.AlmacenPrueba();
        var sesion = new BanosQuimicos.App.Autenticacion.SesionUsuario(api, almacen);
        await sesion.InicializarAsync();
        await sesion.IngresarAsync(new BanosQuimicos.App.Autenticacion.DatosLogin { Email = app.CorreoAdmin, Contrasena = app.ClaveAdmin });
        var detalle = await sesion.DetalleCuentaAsync(objetivo.Id);
        var lista = await sesion.ListarCuentasAsync(1, detalle.Email, "Pendiente");
        Assert.Equal(objetivo.Id, Assert.Single(lista.Elementos).IdUsuario);
        Assert.True(detalle.PuedeActivar);
        await sesion.ActivarCuentaAsync(objetivo.Id);
        var conflicto = await Assert.ThrowsAsync<BanosQuimicos.App.Autenticacion.ErrorApi>(() => sesion.ActivarCuentaAsync(objetivo.Id));
        Assert.Equal(BanosQuimicos.App.Autenticacion.TipoErrorApi.Conflicto, conflicto.Tipo);
        Assert.Equal("Activa", (await sesion.DetalleCuentaAsync(objetivo.Id)).EstadoCuenta);
        Assert.Empty((await sesion.ListarCuentasAsync(1, detalle.Email, "Pendiente")).Elementos);
        Assert.Equal(BanosQuimicos.App.Autenticacion.EstadoSesion.Activa, sesion.Estado);
    }

    [Fact]
    public async Task Cambio_de_rol_quita_gestion_y_la_app_oculta_identidad()
    {
        var actor = await Alta("PRUEBA CUENTAS ADMIN", Guid.NewGuid().ToString("N"), "Activa", "Administrador");
        using var http = Http();
        var almacen = new AutenticacionAppPruebas.AlmacenPrueba { Token = actor.Token };
        var sesion = new BanosQuimicos.App.Autenticacion.SesionUsuario(new BanosQuimicos.App.Autenticacion.ClienteAutenticacion(http), almacen);
        await sesion.InicializarAsync();
        Assert.Equal("Administrador", sesion.Cuenta!.Rol);
        using (var db = app.NuevoContexto())
        {
            var rol = await db.Roles.Where(x => x.NombreRol == "Cliente").Select(x => x.IdRol).SingleAsync(Ct);
            await db.Usuarios.Where(x => x.IdUsuario == actor.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.IdRol, rol), Ct);
        }
        var error = await Assert.ThrowsAsync<BanosQuimicos.App.Autenticacion.ErrorApi>(() => sesion.ListarCuentasAsync(1, null, null));
        Assert.Equal(BanosQuimicos.App.Autenticacion.TipoErrorApi.SinPermiso, error.Tipo);
        Assert.Null(sesion.Cuenta);
        Assert.Equal(BanosQuimicos.App.Autenticacion.EstadoSesion.SinVerificar, sesion.Estado);
        await sesion.ActualizarAsync();
        Assert.Equal("Cliente", sesion.Cuenta!.Rol);
        await Assert.ThrowsAsync<BanosQuimicos.App.Autenticacion.ErrorApi>(() => sesion.DetalleCuentaAsync(actor.Id));
    }

    [Fact]
    public async Task Activaciones_simultaneas_y_repetidas_muestran_estado_actual()
    {
        var cuenta = await Alta("PRUEBA CUENTAS", Guid.NewGuid().ToString("N"));
        using var uno = Http(await Admin()); using var dos = Http(await Admin());
        var prev = (await uno.GetFromJsonAsync<CuentaGestionRespuesta>($"/api/cuentas/{cuenta.Id}", Ct))!;
        Assert.True(prev.PuedeActivar); Assert.Equal("Pendiente", prev.EstadoCuenta);
        var resultados = await Task.WhenAll(uno.PostAsync($"/api/cuentas/{cuenta.Id}/activar", null, Ct), dos.PostAsync($"/api/cuentas/{cuenta.Id}/activar", null, Ct));
        Assert.Single(resultados, x => x.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(resultados, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.Conflict, (await uno.PostAsync($"/api/cuentas/{cuenta.Id}/activar", null, Ct)).StatusCode);
        var actual = (await uno.GetFromJsonAsync<CuentaGestionRespuesta>($"/api/cuentas/{cuenta.Id}", Ct))!;
        Assert.Equal("Activa", actual.EstadoCuenta); Assert.False(actual.PuedeActivar);
        using var db = app.NuevoContexto();
        Assert.Equal(1, await db.Clientes.CountAsync(x => x.IdUsuario == cuenta.Id, Ct));
        Assert.Equal(HttpStatusCode.NotFound, (await uno.GetAsync("/api/cuentas/2147483647", Ct)).StatusCode);
    }
}
