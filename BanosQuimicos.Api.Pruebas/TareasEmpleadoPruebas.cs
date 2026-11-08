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
    internal HttpClient Http(string? token = null)
    {
        var http = app.CreateClient();
        if (token is not null) http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }
    internal async Task<string> Token(string email, string contrasena)
    {
        using var http = Http();
        var r = await http.PostAsJsonAsync("/api/autenticacion/login", new { email, contrasena }, Ct);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<LoginRespuesta>(Ct))!.AccessToken;
    }
    internal async Task<(int Id, string Token, string Email, string Clave)> Usuario(string rol = "Empleado", bool activar = true)
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
        // Una cuenta antigua Pendiente se prepara solo en esta base aislada. El registro real ya es Activa.
        if (!activar) await db.Usuarios.Where(x => x.IdUsuario == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.EstadoCuenta, "Pendiente"), Ct);
        if (rol == "Empleado") Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/cuentas/{id}/rol", new { rolAnterior = "Cliente", nuevoRol = "Empleado" }, Ct)).StatusCode);
        return (id, await Token(email, contrasena), email, contrasena);
    }
    internal async Task<TareaOperativa> Tarea(int? empleado, DateTimeOffset? fecha, bool conServicio = true, int estado = 6)
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
        foreach (var accion in new[] { "registrar-trabajo", "finalizar" })
            Assert.Equal((HttpStatusCode)codigo, (await http.PostAsync("/api/tareas/1/" + accion, null, Ct)).StatusCode);
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
            await Callback(componente, "GuardarAsync", false);
            Assert.Contains("Trabajo registrado por", lista.ToHtmlString());
            Assert.DoesNotContain("Realizado</p>", lista.ToHtmlString());
            await Callback(componente, "AbrirAsync", tarea.IdTarea);
            Assert.Contains("Trabajo registrado por", lista.ToHtmlString());
            await Callback(componente, "GuardarAsync", true);
            Assert.Contains("Realizado</p>", lista.ToHtmlString());
            Assert.Contains("estado-tarea realizada", lista.ToHtmlString());
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

    [Fact]
    public async Task Dos_pasos_persisten_entre_logins_y_repeticiones_no_reemplazan_fechas_ni_campos()
    {
        var a = await Usuario(); var tarea = await Tarea(a.Id, AplicacionTareasPruebas.Ahora, false);
        using var http = Http(a.Token);
        var ruta = $"/api/tareas/{tarea.IdTarea}";
        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsync(ruta + "/finalizar", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.PostAsync(ruta + "/registrar-trabajo", null, Ct)).StatusCode);
        using (var db = app.NuevoContexto())
        {
            var guardada = await db.TareasOperativas.SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct);
            Assert.Equal(6, guardada.IdEstadoOperacion); Assert.Null(guardada.FechaRealizada);
            Assert.Equal(a.Id, guardada.IdEmpleadoRegistroTrabajo); Assert.Equal(AplicacionTareasPruebas.Ahora, guardada.FechaTrabajoRegistrado);
            Assert.Null(guardada.PapelHigienicoRepuesto); Assert.Null(guardada.ObservacionesEmpleado);
        }
        using var nueva = Http(await Token(a.Email, a.Clave));
        var detalle = (await nueva.GetFromJsonAsync<TareaAsignadaRespuesta>(ruta, Ct))!;
        Assert.True(detalle.PuedeFinalizar); Assert.False(detalle.PuedeRegistrar);
        Assert.Equal(HttpStatusCode.OK, (await nueva.PostAsync(ruta + "/finalizar", null, Ct)).StatusCode);
        foreach (var accion in new[] { "registrar-trabajo", "finalizar", "finalizar" })
            Assert.Equal(HttpStatusCode.OK, (await nueva.PostAsync(ruta + "/" + accion, null, Ct)).StatusCode);
        var final = (await nueva.GetFromJsonAsync<TareaAsignadaRespuesta>(ruta, Ct))!;
        Assert.Equal("Realizada", final.Estado); Assert.False(final.PuedeRegistrar); Assert.False(final.PuedeFinalizar);
        Assert.Equal(detalle.FechaTrabajoRegistrado, final.FechaTrabajoRegistrado);
        Assert.Equal(AplicacionTareasPruebas.Ahora, final.FechaRealizada);
        using var dbFinal = app.NuevoContexto();
        Assert.Equal(1, await dbFinal.TareasOperativas.CountAsync(x => x.IdTarea == tarea.IdTarea, Ct));
        Assert.Equal(1, (await dbFinal.BanosQuimicos.SingleAsync(x => x.IdBano == tarea.IdBano, Ct)).IdEstadoBano);
    }

    [Fact]
    public async Task Peticiones_simultaneas_registran_y_finalizan_una_sola_vez()
    {
        var a = await Usuario(); var tarea = await Tarea(a.Id, null, false);
        using var http = Http(a.Token); var ruta = $"/api/tareas/{tarea.IdTarea}";
        foreach (var accion in new[] { "registrar-trabajo", "finalizar" })
        {
            var respuestas = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => http.PostAsync(ruta + "/" + accion, null, Ct)));
            Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
            foreach (var r in respuestas) r.Dispose();
        }
        using var db = app.NuevoContexto();
        var guardada = await db.TareasOperativas.SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct);
        Assert.Equal(a.Id, guardada.IdEmpleadoRegistroTrabajo); Assert.Equal(8, guardada.IdEstadoOperacion);
    }

    [Fact]
    public async Task Otro_empleado_y_reasignacion_no_permiten_apropiarse_del_registro()
    {
        var a = await Usuario(); var b = await Usuario(); var tarea = await Tarea(a.Id, null, false);
        using var uno = Http(a.Token); using var dos = Http(b.Token); var ruta = $"/api/tareas/{tarea.IdTarea}";
        foreach (var accion in new[] { "registrar-trabajo", "finalizar" })
            Assert.Equal(HttpStatusCode.NotFound, (await dos.PostAsync(ruta + "/" + accion, null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await uno.PostAsync(ruta + "/registrar-trabajo", null, Ct)).StatusCode);
        using (var db = app.NuevoContexto())
            await db.TareasOperativas.Where(x => x.IdTarea == tarea.IdTarea).ExecuteUpdateAsync(s => s.SetProperty(x => x.IdEmpleado, b.Id), Ct);
        Assert.Equal(HttpStatusCode.NotFound, (await uno.PostAsync(ruta + "/finalizar", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await dos.PostAsync(ruta + "/finalizar", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await dos.PostAsync(ruta + "/registrar-trabajo", null, Ct)).StatusCode);
        var detalle = (await dos.GetFromJsonAsync<TareaAsignadaRespuesta>(ruta, Ct))!;
        Assert.False(detalle.PuedeRegistrar); Assert.False(detalle.PuedeFinalizar); Assert.Equal(a.Id, detalle.IdEmpleadoRegistroTrabajo);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(8)]
    public async Task Canceladas_y_finalizadas_historicas_no_se_reescriben(int estado)
    {
        var a = await Usuario(); var tarea = await Tarea(a.Id, null, false, estado);
        using var http = Http(a.Token);
        foreach (var accion in new[] { "registrar-trabajo", "finalizar" })
            Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsync($"/api/tareas/{tarea.IdTarea}/" + accion, null, Ct)).StatusCode);
        using var db = app.NuevoContexto();
        var guardada = await db.TareasOperativas.SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct);
        Assert.Equal(estado, guardada.IdEstadoOperacion); Assert.Null(guardada.FechaTrabajoRegistrado);
    }

    [Fact]
    public async Task Solicitud_con_descripcion_solo_resuelve_con_todos_los_trabajos_no_por_cancelar()
    {
        var a = await Usuario(); var tarea = await Tarea(a.Id, null);
        int idSolicitud, segunda;
        using (var db = app.NuevoContexto())
        {
            var servicio = await db.Servicios.SingleAsync(x => x.IdServicio == tarea.IdServicio, Ct);
            var solicitud = new SolicitudCliente { IdCliente = servicio.IdCliente, IdServicioReferencia = servicio.IdServicio,
                IdTipoOperacion = 5, IdEstadoOperacion = 2, FechaSolicitud = AplicacionTareasPruebas.Ahora, Descripcion = "PRUEBA: falta papel; revisar limpieza." };
            var otra = new TareaOperativa { IdBano = tarea.IdBano, IdServicio = tarea.IdServicio, IdEmpleado = a.Id,
                IdTipoOperacion = 3, IdEstadoOperacion = 9, FechaRegistro = AplicacionTareasPruebas.Ahora, Solicitud = solicitud };
            db.TareasOperativas.Add(otra);
            var primera = await db.TareasOperativas.SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct); primera.Solicitud = solicitud;
            await db.SaveChangesAsync(Ct); idSolicitud = solicitud.IdSolicitud; segunda = otra.IdTarea;
        }
        using var http = Http(a.Token); var ruta = $"/api/tareas/{tarea.IdTarea}";
        var detalle = (await http.GetFromJsonAsync<TareaAsignadaRespuesta>(ruta, Ct))!;
        Assert.Equal("Reclamo", detalle.TipoSolicitud); Assert.Contains("falta papel", detalle.DescripcionSolicitud);
        Assert.Equal(HttpStatusCode.OK, (await http.PostAsync(ruta + "/registrar-trabajo", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.PostAsync(ruta + "/finalizar", null, Ct)).StatusCode);
        using (var db = app.NuevoContexto())
        {
            Assert.Equal(2, (await db.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == idSolicitud, Ct)).IdEstadoOperacion);
            // Preparación aislada: representa un segundo trabajo necesario aún pendiente.
            await db.TareasOperativas.Where(x => x.IdTarea == segunda).ExecuteUpdateAsync(s => s.SetProperty(x => x.IdEstadoOperacion, 6), Ct);
        }
        foreach (var accion in new[] { "registrar-trabajo", "finalizar", "finalizar" })
            Assert.Equal(HttpStatusCode.OK, (await http.PostAsync($"/api/tareas/{segunda}/" + accion, null, Ct)).StatusCode);
        using var final = app.NuevoContexto(); var resuelta = await final.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == idSolicitud, Ct);
        Assert.Equal(4, resuelta.IdEstadoOperacion); Assert.Equal(AplicacionTareasPruebas.Ahora, resuelta.FechaResolucion);
    }

    [Theory]
    [InlineData(7, "Entregado")]
    [InlineData(8, "Finalizado")]
    public async Task Cierre_entrega_o_retiro_actualiza_servicio_una_vez(int tipo, string resultado)
    {
        var a = await Usuario(); var tarea = await Tarea(a.Id, null);
        using (var db = app.NuevoContexto())
        {
            var t = await db.TareasOperativas.Include(x => x.Servicio).SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct);
            t.IdTipoOperacion = tipo;
            if (tipo == 7) { t.Servicio!.EstadoServicio = "Pendiente de entrega"; t.Servicio.FechaEntregaReal = null; }
            await db.SaveChangesAsync(Ct);
        }
        using var http = Http(a.Token);
        foreach (var accion in new[] { "registrar-trabajo", "finalizar", "finalizar" })
            Assert.Equal(HttpStatusCode.OK, (await http.PostAsync($"/api/tareas/{tarea.IdTarea}/" + accion, null, Ct)).StatusCode);
        using var final = app.NuevoContexto(); var servicio = await final.Servicios.SingleAsync(x => x.IdServicio == tarea.IdServicio, Ct);
        Assert.Equal(resultado, servicio.EstadoServicio);
        if (tipo == 7) Assert.Equal(AplicacionTareasPruebas.Ahora, servicio.FechaEntregaReal);
        else { Assert.Equal(AplicacionTareasPruebas.Ahora, servicio.FechaRetiroReal); Assert.Equal(servicio.FechaRetiroReal, servicio.FechaFinalizacion); }
    }

    [Fact]
    public async Task Traslado_cierra_ubicacion_anterior_sin_redirigir_trabajo_historico()
    {
        var a = await Usuario(); var tarea = await Tarea(a.Id, null); int origenId, destinoId;
        using (var db = app.NuevoContexto())
        {
            var t = await db.TareasOperativas.SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct);
            var origen = await db.UbicacionesServicio.SingleAsync(x => x.IdServicio == tarea.IdServicio && x.EsActual, Ct);
            var destino = new UbicacionServicio { IdServicio = tarea.IdServicio!.Value, Direccion = "PRUEBA DESTINO", FechaRegistro = AplicacionTareasPruebas.Ahora };
            db.UbicacionesServicio.Add(destino); await db.SaveChangesAsync(Ct);
            t.IdTipoOperacion = 10; t.IdUbicacion = origen.IdUbicacion; t.IdUbicacionDestino = destino.IdUbicacion;
            await db.SaveChangesAsync(Ct); origenId = origen.IdUbicacion; destinoId = destino.IdUbicacion;
        }
        using var http = Http(a.Token);
        foreach (var accion in new[] { "registrar-trabajo", "finalizar", "finalizar" })
            Assert.Equal(HttpStatusCode.OK, (await http.PostAsync($"/api/tareas/{tarea.IdTarea}/" + accion, null, Ct)).StatusCode);
        using var final = app.NuevoContexto();
        var anterior = await final.UbicacionesServicio.SingleAsync(x => x.IdUbicacion == origenId, Ct);
        var actual = await final.UbicacionesServicio.SingleAsync(x => x.IdUbicacion == destinoId, Ct);
        Assert.False(anterior.EsActual); Assert.True(actual.EsActual); Assert.Equal(anterior.FechaHasta, actual.FechaDesde);
        Assert.Equal(AplicacionTareasPruebas.Ahora, actual.FechaDesde);
        Assert.Equal(origenId, (await final.TareasOperativas.SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct)).IdUbicacion);
    }

    [Fact]
    public async Task Regresion_traslado_muestra_origen_y_destino_en_api_y_detalle_antes_y_despues()
    {
        var a = await Usuario(); var tarea = await Tarea(a.Id, null); int origenId, destinoId;
        using (var db = app.NuevoContexto())
        {
            var t = await db.TareasOperativas.SingleAsync(t => t.IdTarea == tarea.IdTarea, Ct);
            var origen = await db.UbicacionesServicio.SingleAsync(u => u.IdServicio == tarea.IdServicio && u.EsActual, Ct);
            var destino = new UbicacionServicio { IdServicio = tarea.IdServicio!.Value, Direccion = "REGRESION DESTINO TRASLADO 456", FechaRegistro = AplicacionTareasPruebas.Ahora };
            db.UbicacionesServicio.Add(destino); await db.SaveChangesAsync(Ct);
            t.IdTipoOperacion = 10; t.IdUbicacion = origen.IdUbicacion; t.IdUbicacionDestino = destino.IdUbicacion;
            await db.SaveChangesAsync(Ct); origenId = origen.IdUbicacion; destinoId = destino.IdUbicacion;
        }
        using var http = Http();
        var sesion = new ClienteApp.SesionUsuario(new ClienteApp.ClienteAutenticacion(http), new AutenticacionAppPruebas.AlmacenPrueba { Token = a.Token });
        await sesion.InicializarAsync();
        var captura = new Captura();
        await using var proveedor = new ServiceCollection().AddLogging().AddSingleton(sesion).AddSingleton<IComponentActivator>(captura).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(proveedor, proveedor.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var vista = await renderer.RenderComponentAsync<Vistas.TareasAsignadas>();
            var componente = captura.Instancias[typeof(Vistas.TareasAsignadas)];
            await Callback(componente, "AbrirAsync", tarea.IdTarea);
            void Verificar()
            {
                var html = WebUtility.HtmlDecode(vista.ToHtmlString());
                Assert.Contains("Dirección de origen", html); Assert.Contains("PRUEBA OTRA DIRECCION ACTUAL", html);
                Assert.Contains("Dirección de destino", html); Assert.Contains("REGRESION DESTINO TRASLADO 456", html);
                Assert.Contains($"<dd>{tarea.IdServicio}</dd>", html);
            }
            Verificar();
            await Callback(componente, "GuardarAsync", false); await Callback(componente, "GuardarAsync", true);
            await Callback(componente, "AbrirAsync", tarea.IdTarea); Verificar();
        });
        using var autenticado = Http(a.Token);
        var dto = (await autenticado.GetFromJsonAsync<TareaAsignadaRespuesta>($"/api/tareas/{tarea.IdTarea}", Ct))!;
        Assert.Equal(origenId, dto.IdUbicacionOrigen); Assert.Equal(destinoId, dto.IdUbicacionDestino);
        Assert.Equal("PRUEBA OTRA DIRECCION ACTUAL", dto.DireccionOrigen); Assert.Equal("REGRESION DESTINO TRASLADO 456", dto.DireccionDestino);
        using var final = app.NuevoContexto();
        Assert.False((await final.UbicacionesServicio.SingleAsync(u => u.IdUbicacion == origenId, Ct)).EsActual);
        Assert.True((await final.UbicacionesServicio.SingleAsync(u => u.IdUbicacion == destinoId, Ct)).EsActual);
    }

    [Fact]
    public async Task Pedido_multibano_no_resuelve_por_completar_una_sola_entrega()
    {
        var a = await Usuario(); var tarea = await Tarea(a.Id, null); int solicitudId;
        using (var db = app.NuevoContexto())
        {
            var t = await db.TareasOperativas.Include(x => x.Servicio).SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct);
            var solicitud = new SolicitudCliente { IdCliente = t.Servicio!.IdCliente, IdTipoOperacion = 1, IdEstadoOperacion = 2,
                CantidadBanos = 2, FechaSolicitud = AplicacionTareasPruebas.Ahora };
            t.Solicitud = solicitud; t.IdTipoOperacion = 7; t.Servicio.SolicitudOrigen = solicitud;
            t.Servicio.EstadoServicio = "Pendiente de entrega"; t.Servicio.FechaEntregaReal = null;
            await db.SaveChangesAsync(Ct); solicitudId = solicitud.IdSolicitud;
        }
        using var http = Http(a.Token);
        foreach (var accion in new[] { "registrar-trabajo", "finalizar" })
            Assert.Equal(HttpStatusCode.OK, (await http.PostAsync($"/api/tareas/{tarea.IdTarea}/" + accion, null, Ct)).StatusCode);
        using var final = app.NuevoContexto();
        Assert.Equal(2, (await final.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == solicitudId, Ct)).IdEstadoOperacion);
    }

    [Fact]
    public async Task Cancelacion_entre_pasos_impide_finalizar_y_conserva_el_registro()
    {
        var a = await Usuario(); var tarea = await Tarea(a.Id, null, false, 7);
        using var http = Http(a.Token); var ruta = $"/api/tareas/{tarea.IdTarea}";
        Assert.Equal(HttpStatusCode.OK, (await http.PostAsync(ruta + "/registrar-trabajo", null, Ct)).StatusCode);
        using (var db = app.NuevoContexto())
        {
            var t = await db.TareasOperativas.SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct);
            Assert.Equal(7, t.IdEstadoOperacion);
            t.IdEstadoOperacion = 9; t.FechaCancelacion = AplicacionTareasPruebas.Ahora;
            await db.SaveChangesAsync(Ct);
        }
        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsync(ruta + "/finalizar", null, Ct)).StatusCode);
        using var final = app.NuevoContexto();
        var guardada = await final.TareasOperativas.SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct);
        Assert.Equal(9, guardada.IdEstadoOperacion); Assert.Equal(a.Id, guardada.IdEmpleadoRegistroTrabajo); Assert.Null(guardada.FechaRealizada);
    }

    [Fact]
    public async Task Error_de_cierre_no_guarda_estado_ni_efectos_parciales()
    {
        var a = await Usuario(); var tarea = await Tarea(a.Id, null);
        using (var db = app.NuevoContexto())
        {
            var t = await db.TareasOperativas.SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct);
            t.IdTipoOperacion = 10; // Sin destino: debe revisarlo administración, no inventar uno.
            await db.SaveChangesAsync(Ct);
        }
        using var http = Http(a.Token);
        Assert.Equal(HttpStatusCode.OK, (await http.PostAsync($"/api/tareas/{tarea.IdTarea}/registrar-trabajo", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await http.PostAsync($"/api/tareas/{tarea.IdTarea}/finalizar", null, Ct)).StatusCode);
        using var final = app.NuevoContexto();
        var guardada = await final.TareasOperativas.SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct);
        Assert.Equal(6, guardada.IdEstadoOperacion); Assert.Null(guardada.FechaRealizada); Assert.NotNull(guardada.FechaTrabajoRegistrado);
        Assert.Equal(1, await final.UbicacionesServicio.CountAsync(x => x.IdServicio == tarea.IdServicio && x.EsActual, Ct));
    }

    [Fact]
    public async Task Dos_tareas_de_una_solicitud_finalizan_simultaneamente_y_resuelven_una_vez()
    {
        var a = await Usuario(); var b = await Usuario(); var tarea = await Tarea(a.Id, null); int segundaId, solicitudId;
        using (var db = app.NuevoContexto())
        {
            var primera = await db.TareasOperativas.Include(x => x.Servicio).SingleAsync(x => x.IdTarea == tarea.IdTarea, Ct);
            var solicitud = new SolicitudCliente { IdCliente = primera.Servicio!.IdCliente, IdServicioReferencia = tarea.IdServicio,
                IdTipoOperacion = 3, IdEstadoOperacion = 2, FechaSolicitud = AplicacionTareasPruebas.Ahora };
            primera.Solicitud = solicitud;
            var segunda = new TareaOperativa { IdEmpleado = b.Id, IdBano = tarea.IdBano, IdServicio = tarea.IdServicio,
                IdTipoOperacion = 3, IdEstadoOperacion = 6, FechaRegistro = AplicacionTareasPruebas.Ahora, Solicitud = solicitud };
            db.TareasOperativas.Add(segunda); await db.SaveChangesAsync(Ct); segundaId = segunda.IdTarea; solicitudId = solicitud.IdSolicitud;
        }
        using var uno = Http(a.Token); using var dos = Http(b.Token);
        Assert.Equal(HttpStatusCode.OK, (await uno.PostAsync($"/api/tareas/{tarea.IdTarea}/registrar-trabajo", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await dos.PostAsync($"/api/tareas/{segundaId}/registrar-trabajo", null, Ct)).StatusCode);
        var respuestas = await Task.WhenAll(uno.PostAsync($"/api/tareas/{tarea.IdTarea}/finalizar", null, Ct), dos.PostAsync($"/api/tareas/{segundaId}/finalizar", null, Ct));
        Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        foreach (var r in respuestas) r.Dispose();
        using var final = app.NuevoContexto();
        Assert.Equal(4, (await final.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == solicitudId, Ct)).IdEstadoOperacion);
        Assert.Equal(2, await final.TareasOperativas.CountAsync(x => x.IdSolicitud == solicitudId && x.IdEstadoOperacion == 8, Ct));
    }
}
