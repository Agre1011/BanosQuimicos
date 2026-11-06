using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Reflection;
using BanosQuimicos.Api.Contratos.Autenticacion;
using BanosQuimicos.Api.Contratos.Tareas;
using BanosQuimicos.Api.Modelos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Xunit;
using ClienteApp = global::BanosQuimicos.App.Autenticacion;
using Vistas = global::BanosQuimicos.App.Components.Empleados;

namespace BanosQuimicos.Api.Pruebas;

public class AplicacionTareasPruebas : AplicacionPruebas
{
    public static readonly DateTimeOffset Ahora = DateTimeOffset.Parse("2030-04-05T01:30:00Z");
    private sealed class RelojFijo : TimeProvider { public override DateTimeOffset GetUtcNow() => Ahora; }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(s => s.AddSingleton<TimeProvider>(new RelojFijo()));
    }
}

public class TareasEmpleadoPruebas(AplicacionTareasPruebas app) : IClassFixture<AplicacionTareasPruebas>
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private HttpClient Http(string? token = null)
    {
        var http = app.CreateClient();
        if (token is not null) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }
    private async Task<string> Token(string email, string contrasena)
    {
        using var http = Http();
        var r = await http.PostAsJsonAsync("/api/autenticacion/login", new { email, contrasena }, Ct);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<LoginRespuesta>(Ct))!.AccessToken;
    }
    private async Task<(int Id, string Token)> Usuario(string rol = "Empleado", bool activar = true)
    {
        var email = "prueba-tareas-" + Guid.NewGuid().ToString("N") + "@example.invalid";
        var contrasena = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        using var http = Http();
        Assert.Equal(HttpStatusCode.Created, (await http.PostAsJsonAsync("/api/autenticacion/registro", new RegistroClienteSolicitud {
            Nombre = "PRUEBA TAREAS", Apellido = Guid.NewGuid().ToString("N"), Email = email, Telefono = "+00 PRUEBA", Contrasena = contrasena
        }, Ct)).StatusCode);
        using var db = app.NuevoContexto();
        var id = await db.Usuarios.Where(x => x.Email == email).Select(x => x.IdUsuario).SingleAsync(Ct);
        using var admin = Http(await Token(app.CorreoAdmin, app.ClaveAdmin));
        if (activar) Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/cuentas/{id}/activar", null, Ct)).StatusCode);
        if (rol == "Empleado") Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/cuentas/{id}/rol", new { rolAnterior = "Cliente", nuevoRol = "Empleado" }, Ct)).StatusCode);
        return (id, await Token(email, contrasena));
    }
    private async Task<TareaOperativa> Tarea(int? empleado, DateTimeOffset? fecha, bool conServicio = true, int estado = 6)
    {
        using var db = app.NuevoContexto();
        var bano = new BanoQuimico { CodigoInterno = "PT-" + Guid.NewGuid().ToString("N")[..10], IdTipoBano = 1, IdEstadoBano = 1, FechaAlta = AplicacionTareasPruebas.Ahora.AddDays(-10) };
        var tarea = new TareaOperativa { Bano = bano, IdEmpleado = empleado, FechaRegistro = AplicacionTareasPruebas.Ahora,
            FechaProgramada = fecha, IdTipoOperacion = conServicio ? 3 : 11, IdEstadoOperacion = estado };
        if (conServicio)
        {
            var cliente = await Usuario("Cliente");
            var idCliente = await db.Clientes.Where(x => x.IdUsuario == cliente.Id).Select(x => x.IdCliente).SingleAsync(Ct);
            var servicio = new Servicio { Bano = bano, IdCliente = idCliente, EstadoServicio = "Entregado", TipoUso = "Obra",
                FechaAsignacion = AplicacionTareasPruebas.Ahora.AddDays(-8), FechaEntregaReal = AplicacionTareasPruebas.Ahora.AddDays(-7) };
            tarea.Servicio = servicio;
            var antigua = new UbicacionServicio { Servicio = servicio, Direccion = "PRUEBA DIRECCION HISTORICA", FechaRegistro = AplicacionTareasPruebas.Ahora.AddDays(-7), FechaDesde = AplicacionTareasPruebas.Ahora.AddDays(-7), FechaHasta = AplicacionTareasPruebas.Ahora, EsActual = false };
            db.UbicacionesServicio.Add(new UbicacionServicio { Servicio = servicio, Direccion = "PRUEBA OTRA DIRECCION ACTUAL", FechaRegistro = AplicacionTareasPruebas.Ahora, FechaDesde = AplicacionTareasPruebas.Ahora, EsActual = true });
            tarea.Ubicacion = antigua;
        }
        db.TareasOperativas.Add(tarea); await db.SaveChangesAsync(Ct);
        return tarea;
    }

    [Fact]
    public async Task Dos_empleados_solo_ven_sus_tareas_y_no_pueden_leer_detalle_ajeno()
    {
        var a = await Usuario(); var b = await Usuario();
        var propia = await Tarea(a.Id, AplicacionTareasPruebas.Ahora);
        var ajena = await Tarea(b.Id, AplicacionTareasPruebas.Ahora);
        await Tarea(null, AplicacionTareasPruebas.Ahora, false);
        using var uno = Http(a.Token); using var dos = Http(b.Token);
        var listaA = (await uno.GetFromJsonAsync<List<TareaAsignadaRespuesta>>("/api/tareas/mias?idEmpleado=" + b.Id, Ct))!;
        Assert.Equal(propia.IdTarea, Assert.Single(listaA).IdTarea);
        Assert.Equal(ajena.IdTarea, Assert.Single((await dos.GetFromJsonAsync<List<TareaAsignadaRespuesta>>("/api/tareas/mias", Ct))!).IdTarea);
        Assert.Equal(HttpStatusCode.NotFound, (await uno.GetAsync($"/api/tareas/{ajena.IdTarea}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await dos.GetAsync($"/api/tareas/{propia.IdTarea}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await uno.GetAsync("/api/tareas/2147483647", Ct)).StatusCode);
        foreach (var metodo in new[] { HttpMethod.Put, HttpMethod.Patch, HttpMethod.Post, HttpMethod.Delete })
        {
            using var request = new HttpRequestMessage(metodo, $"/api/tareas/{ajena.IdTarea}") { Content = JsonContent.Create(new { idEstadoOperacion = 8, observacionesEmpleado = "INTENTO" }) };
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await uno.SendAsync(request, Ct)).StatusCode);
        }
        using var db = app.NuevoContexto();
        var intacta = await db.TareasOperativas.SingleAsync(x => x.IdTarea == ajena.IdTarea, Ct);
        Assert.Equal(6, intacta.IdEstadoOperacion); Assert.Null(intacta.ObservacionesEmpleado);
    }

    [Fact]
    public async Task Cuenta_hoy_usa_programacion_argentina_y_no_fecha_registro_ni_otro_empleado()
    {
        var a = await Usuario(); var b = await Usuario();
        // Ahora es 5/4 UTC pero todavía 4/4 en Argentina.
        var inicio = DateTimeOffset.Parse("2030-04-04T00:00:00-03:00");
        await Tarea(a.Id, inicio, false);
        await Tarea(a.Id, inicio.AddDays(1).AddTicks(-1), false, 9);
        await Tarea(a.Id, inicio.AddTicks(-1), false);
        await Tarea(a.Id, inicio.AddDays(1), false);
        await Tarea(a.Id, null, false);
        await Tarea(b.Id, inicio, false);
        using var http = Http(a.Token);
        var resumen = (await http.GetFromJsonAsync<ResumenTareasRespuesta>("/api/tareas/mias/resumen", Ct))!;
        Assert.Equal(new DateOnly(2030, 4, 4), resumen.Fecha); Assert.Equal(2, resumen.Cantidad);
    }

    [Fact]
    public async Task Detalle_conserva_ubicacion_de_tarea_y_admite_mantenimiento_sin_servicio()
    {
        var a = await Usuario(); var t = await Tarea(a.Id, AplicacionTareasPruebas.Ahora);
        var mantenimiento = await Tarea(a.Id, null, false);
        using var http = Http(a.Token);
        var detalle = (await http.GetFromJsonAsync<TareaAsignadaRespuesta>($"/api/tareas/{t.IdTarea}", Ct))!;
        Assert.Equal("PRUEBA DIRECCION HISTORICA", detalle.Direccion);
        Assert.Equal("Limpieza extra", detalle.Tipo); Assert.Equal("Pendiente", detalle.Estado);
        Assert.StartsWith("PRUEBA TAREAS", detalle.Cliente); Assert.Equal(t.Bano.CodigoInterno, detalle.CodigoBano);
        var sinServicio = (await http.GetFromJsonAsync<TareaAsignadaRespuesta>($"/api/tareas/{mantenimiento.IdTarea}", Ct))!;
        Assert.Null(sinServicio.Cliente); Assert.Null(sinServicio.Direccion); Assert.Null(sinServicio.FechaProgramada);
        var json = await http.GetStringAsync($"/api/tareas/{t.IdTarea}", Ct);
        Assert.DoesNotContain("hash", json, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Anonimo", 401)]
    [InlineData("Cliente", 403)]
    [InlineData("Administrador", 403)]
    [InlineData("Pendiente", 403)]
    [InlineData("Suspendida", 401)]
    [InlineData("BajaLogica", 401)]
    public async Task Consultas_requieren_empleado_actual_activo(string caso, int codigo)
    {
        string? token = null;
        if (caso == "Administrador") token = await Token(app.CorreoAdmin, app.ClaveAdmin);
        else if (caso != "Anonimo")
        {
            var usuario = await Usuario(caso == "Cliente" ? "Cliente" : "Empleado", caso != "Pendiente");
            token = usuario.Token;
            if (caso is "Suspendida" or "BajaLogica")
            {
                using var db = app.NuevoContexto();
                await db.Usuarios.Where(x => x.IdUsuario == usuario.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.EstadoCuenta, caso), Ct);
            }
        }
        using var http = Http(token);
        foreach (var ruta in new[] { "/api/tareas/mias", "/api/tareas/mias/resumen", "/api/tareas/1" })
            Assert.Equal((HttpStatusCode)codigo, (await http.GetAsync(ruta, Ct)).StatusCode);
    }

    [Fact]
    public async Task Sin_tareas_devuelve_lista_vacia_y_contador_cero_y_cambio_rol_quita_acceso()
    {
        var a = await Usuario(); using var http = Http(a.Token);
        Assert.Empty((await http.GetFromJsonAsync<List<TareaAsignadaRespuesta>>("/api/tareas/mias", Ct))!);
        Assert.Equal(0, (await http.GetFromJsonAsync<ResumenTareasRespuesta>("/api/tareas/mias/resumen", Ct))!.Cantidad);
        using var admin = Http(await Token(app.CorreoAdmin, app.ClaveAdmin));
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/cuentas/{a.Id}/rol", new { rolAnterior = "Empleado", nuevoRol = "Cliente" }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/tareas/mias", Ct)).StatusCode);
    }

    private sealed class Captura : IComponentActivator
    {
        public readonly Dictionary<Type, IComponent> Instancias = [];
        public IComponent CreateInstance(Type tipo) { var c = (IComponent)Activator.CreateInstance(tipo)!; Instancias[tipo] = c; return c; }
    }
    private static async Task Callback(object componente, string nombre, params object?[] args)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var valor = componente.GetType().GetMethod(nombre, flags)!.Invoke(componente, args);
        if (valor is Task t) await t;
        typeof(ComponentBase).GetMethod("StateHasChanged", flags)!.Invoke(componente, null);
    }

    [Fact]
    public async Task Razor_muestra_contador_lista_detalle_vuelta_y_caso_vacio()
    {
        var a = await Usuario(); var tarea = await Tarea(a.Id, AplicacionTareasPruebas.Ahora);
        using var http = Http();
        var sesion = new ClienteApp.SesionUsuario(new ClienteApp.ClienteAutenticacion(http), new AutenticacionAppPruebas.AlmacenPrueba { Token = a.Token });
        await sesion.InicializarAsync();
        var captura = new Captura();
        var servicios = new ServiceCollection().AddLogging().AddSingleton(sesion).AddSingleton<IComponentActivator>(captura);
        await using var proveedor = servicios.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(proveedor, proveedor.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var inicio = await renderer.RenderComponentAsync<Vistas.InicioEmpleado>();
            Assert.Contains("1 tarea(s) programada(s)", inicio.ToHtmlString());
            var lista = await renderer.RenderComponentAsync<Vistas.TareasAsignadas>();
            Assert.Contains(tarea.Bano.CodigoInterno, lista.ToHtmlString());
            var componente = captura.Instancias[typeof(Vistas.TareasAsignadas)];
            await Callback(componente, "AbrirAsync", tarea.IdTarea);
            var detalle = lista.ToHtmlString();
            Assert.Contains("PRUEBA DIRECCION HISTORICA", detalle); Assert.Contains("Pendiente", detalle);
            Assert.Contains("Registrar Trabajo", detalle); Assert.Contains("Actualizar Estado", detalle);
            Assert.Contains("disabled", detalle); Assert.Contains("Volver", detalle);
            await Callback(componente, "CargarAsync");
            Assert.Contains("Tareas asignadas", lista.ToHtmlString());
            Assert.DoesNotContain("Registrar Trabajo", lista.ToHtmlString());
        });
        var vacio = await Usuario();
        using var httpVacio = Http();
        var otraSesion = new ClienteApp.SesionUsuario(new ClienteApp.ClienteAutenticacion(httpVacio), new AutenticacionAppPruebas.AlmacenPrueba { Token = vacio.Token });
        await otraSesion.InicializarAsync();
        await using var proveedorVacio = new ServiceCollection().AddLogging().AddSingleton(otraSesion).BuildServiceProvider();
        await using var rendererVacio = new HtmlRenderer(proveedorVacio, proveedorVacio.GetRequiredService<ILoggerFactory>());
        await rendererVacio.Dispatcher.InvokeAsync(async () =>
        {
            Assert.Contains("0 tarea(s)", (await rendererVacio.RenderComponentAsync<Vistas.InicioEmpleado>()).ToHtmlString());
            Assert.Contains("No ten", (await rendererVacio.RenderComponentAsync<Vistas.TareasAsignadas>()).ToHtmlString());
        });
    }
}
