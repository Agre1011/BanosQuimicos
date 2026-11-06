using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using BanosQuimicos.Api.Contratos.Banos;
using BanosQuimicos.Api.Contratos.Tareas;
using BanosQuimicos.Api.Modelos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using ClienteApp = global::BanosQuimicos.App.Autenticacion;

namespace BanosQuimicos.Api.Pruebas;

public class EstadoBanosPruebas(AplicacionTareasPruebas app) : IClassFixture<AplicacionTareasPruebas>
{
    private readonly TareasEmpleadoPruebas datos = new(app);
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private async Task<HttpClient> Admin() => datos.Http(await datos.Token(app.CorreoAdmin, app.ClaveAdmin));
    private static ProgramarLimpiezaSolicitud Pedido(TareaOperativa t, int empleado) => new() {
        ClaveProgramacion = Guid.NewGuid(), IdEmpleado = empleado, IdServicio = t.IdServicio!.Value,
        IdUbicacion = t.IdUbicacion!.Value, Fecha = new DateOnly(2030, 4, 4)
    };
    private async Task<ProgramarLimpiezaSolicitud> PedidoActual(TareaOperativa t, int empleado)
    {
        var pedido = Pedido(t, empleado);
        using var db = app.NuevoContexto();
        pedido.IdUbicacion = await db.UbicacionesServicio.Where(x => x.IdServicio == t.IdServicio && x.EsActual).Select(x => x.IdUbicacion).SingleAsync(Ct);
        return pedido;
    }

    [Fact]
    public async Task Administrador_programa_para_otro_empleado_sin_cambiar_responsable_y_empleado_cumple()
    {
        var a = await datos.Usuario(); var b = await datos.Usuario(); var vieja = await datos.Tarea(a.Id, null);
        using var admin = await Admin();
        var respuesta = await admin.PutAsJsonAsync($"/api/banos/{vieja.IdBano}/responsable-limpieza", new { idEmpleado = a.Id }, Ct);
        Assert.True(respuesta.StatusCode == HttpStatusCode.OK, (await respuesta.Content.ReadAsStringAsync(Ct)).Split("HEADERS")[0]);
        var estado = (await respuesta.Content.ReadFromJsonAsync<EstadoBanoRespuesta>(Ct))!;
        Assert.Equal(a.Id, estado.IdResponsable); Assert.True(estado.ResponsableActivo);
        Assert.False(estado.Disponible); Assert.Equal("Entregado", estado.EstadoServicio);
        var pedido = await PedidoActual(vieja, b.Id);
        var creada = await admin.PostAsJsonAsync($"/api/banos/{vieja.IdBano}/limpiezas", pedido, Ct);
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        var id = (await creada.Content.ReadFromJsonAsync<ClienteApp.LimpiezaProgramada>(Ct))!.IdTarea;
        using var empleado = datos.Http(b.Token); using var habitual = datos.Http(a.Token);
        var lista = (await empleado.GetFromJsonAsync<List<TareaAsignadaRespuesta>>("/api/tareas/mias", Ct))!;
        Assert.Equal(id, Assert.Single(lista).IdTarea); Assert.Equal("Limpieza semanal", lista[0].Tipo);
        Assert.Equal("PRUEBA OTRA DIRECCION ACTUAL", lista[0].Direccion);
        Assert.Equal(1, (await empleado.GetFromJsonAsync<ResumenTareasRespuesta>("/api/tareas/mias/resumen", Ct))!.Cantidad);
        Assert.Equal(HttpStatusCode.NotFound, (await habitual.GetAsync($"/api/tareas/{id}", Ct)).StatusCode);
        foreach (var accion in new[] { "registrar-trabajo", "finalizar" })
            Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{id}/" + accion, null, Ct)).StatusCode);
        using var db = app.NuevoContexto();
        Assert.Equal(a.Id, (await db.BanosQuimicos.SingleAsync(x => x.IdBano == vieja.IdBano, Ct)).IdResponsableLimpieza);
        Assert.Equal(a.Id, (await db.TareasOperativas.SingleAsync(x => x.IdTarea == vieja.IdTarea, Ct)).IdEmpleado);
        var final = await db.TareasOperativas.SingleAsync(x => x.IdTarea == id, Ct);
        Assert.Equal(8, final.IdEstadoOperacion); Assert.Equal(b.Id, final.IdEmpleadoRegistroTrabajo);
        Assert.Null(final.IdSolicitud); Assert.Null(final.ObservacionesEmpleado); Assert.Null(final.PapelHigienicoRepuesto);
        Assert.Equal(TimeSpan.FromHours(-3), final.FechaProgramada!.Value.Offset);
    }

    [Fact]
    public async Task Repeticiones_y_envios_concurrentes_no_duplican_programacion()
    {
        var a = await datos.Usuario(); var t = await datos.Tarea(a.Id, null);
        var pedido = await PedidoActual(t, a.Id); using var admin = await Admin();
        var ruta = $"/api/banos/{t.IdBano}/limpiezas";
        var respuestas = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => admin.PostAsJsonAsync(ruta, pedido, Ct)));
        Assert.Single(respuestas, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Equal(3, respuestas.Count(x => x.StatusCode == HttpStatusCode.OK));
        var ids = new List<int>();
        foreach (var r in respuestas) { ids.Add((await r.Content.ReadFromJsonAsync<ClienteApp.LimpiezaProgramada>(Ct))!.IdTarea); r.Dispose(); }
        Assert.Single(ids.Distinct());
        pedido.Fecha = pedido.Fecha.AddDays(1);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, pedido, Ct)).StatusCode);
        pedido.Fecha = pedido.Fecha.AddDays(-1); pedido.ClaveProgramacion = Guid.NewGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, pedido, Ct)).StatusCode);
        using var db = app.NuevoContexto();
        Assert.Equal(1, await db.TareasOperativas.CountAsync(x => x.IdBano == t.IdBano && x.IdTipoOperacion == 9, Ct));
        Assert.Null((await db.BanosQuimicos.SingleAsync(x => x.IdBano == t.IdBano, Ct)).IdResponsableLimpieza);
    }

    [Fact]
    public async Task Responsable_concurrente_no_pisa_otro_y_cambiarlo_no_reasigna_historial()
    {
        var a = await datos.Usuario(); var b = await datos.Usuario(); var t = await datos.Tarea(a.Id, null);
        using var admin = await Admin(); var ruta = $"/api/banos/{t.IdBano}/responsable-limpieza";
        var respuestas = await Task.WhenAll(admin.PutAsJsonAsync(ruta, new { idEmpleado = a.Id }, Ct), admin.PutAsJsonAsync(ruta, new { idEmpleado = b.Id }, Ct));
        Assert.Single(respuestas, x => x.StatusCode == HttpStatusCode.OK); Assert.Single(respuestas, x => x.StatusCode == HttpStatusCode.Conflict);
        foreach (var r in respuestas) r.Dispose();
        using var db = app.NuevoContexto(); var actual = await db.BanosQuimicos.SingleAsync(x => x.IdBano == t.IdBano, Ct);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync(ruta, new { idEmpleado = actual.IdResponsableLimpieza }, Ct)).StatusCode);
        Assert.Equal(a.Id, (await db.TareasOperativas.SingleAsync(x => x.IdTarea == t.IdTarea, Ct)).IdEmpleado);
    }

    [Theory]
    [InlineData("Anonimo", 401)]
    [InlineData("Cliente", 403)]
    [InlineData("Empleado", 403)]
    [InlineData("Pendiente", 403)]
    [InlineData("Suspendida", 401)]
    public async Task Solo_administrador_activo_puede_consultar_y_escribir(string caso, int codigo)
    {
        string? token = null;
        if (caso != "Anonimo")
        {
            var usuario = await datos.Usuario(caso == "Cliente" ? "Cliente" : "Empleado", caso != "Pendiente"); token = usuario.Token;
            if (caso == "Suspendida")
            {
                using var db = app.NuevoContexto();
                await db.Usuarios.Where(x => x.IdUsuario == usuario.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.EstadoCuenta, "Suspendida"), Ct);
            }
        }
        using var http = datos.Http(token);
        foreach (var ruta in new[] { "/api/banos/estado", "/api/banos/empleados-asignables" })
            Assert.Equal((HttpStatusCode)codigo, (await http.GetAsync(ruta, Ct)).StatusCode);
        Assert.Equal((HttpStatusCode)codigo, (await http.PutAsJsonAsync("/api/banos/1/responsable-limpieza", new { idEmpleado = 1 }, Ct)).StatusCode);
        Assert.Equal((HttpStatusCode)codigo, (await http.PostAsJsonAsync("/api/banos/1/limpiezas", new ProgramarLimpiezaSolicitud { IdEmpleado = 1, IdServicio = 1, IdUbicacion = 1, ClaveProgramacion = Guid.NewGuid(), Fecha = new(2030, 4, 4) }, Ct)).StatusCode);
    }

    [Theory]
    [InlineData("Cliente")]
    [InlineData("Pendiente")]
    [InlineData("Suspendida")]
    [InlineData("BajaLogica")]
    public async Task Seleccion_de_empleado_rechaza_roles_y_cuentas_no_habilitadas(string caso)
    {
        var usuario = await datos.Usuario(caso == "Cliente" ? "Cliente" : "Empleado", caso != "Pendiente");
        var tarea = await datos.Tarea(null, null); using var admin = await Admin();
        if (caso is "Suspendida" or "BajaLogica")
        {
            using var db = app.NuevoContexto();
            await db.Usuarios.Where(x => x.IdUsuario == usuario.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.EstadoCuenta, caso), Ct);
        }
        var lista = (await admin.GetFromJsonAsync<List<EmpleadoAsignableRespuesta>>("/api/banos/empleados-asignables", Ct))!;
        Assert.DoesNotContain(lista, x => x.IdUsuario == usuario.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PutAsJsonAsync($"/api/banos/{tarea.IdBano}/responsable-limpieza", new { idEmpleado = usuario.Id }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/banos/{tarea.IdBano}/limpiezas", await PedidoActual(tarea, usuario.Id), Ct)).StatusCode);
    }

    [Fact]
    public async Task Disponibilidad_no_se_confunde_con_condicion_y_limpieza_no_crea_servicio()
    {
        var a = await datos.Usuario(); var libre = await datos.Tarea(null, null, false);
        var ocupado = await datos.Tarea(a.Id, null); using var admin = await Admin();
        async Task<EstadoBanoRespuesta> Estado(int id) => (await admin.GetFromJsonAsync<List<EstadoBanoRespuesta>>("/api/banos/estado", Ct))!.Single(x => x.IdBano == id);
        Assert.True((await Estado(libre.IdBano)).Disponible); // Mantenimiento no bloqueante pendiente.
        using (var db = app.NuevoContexto())
        {
            await db.BanosQuimicos.Where(x => x.IdBano == ocupado.IdBano).ExecuteUpdateAsync(s => s.SetProperty(x => x.IdEstadoBano, 2), Ct);
            await db.TareasOperativas.Where(x => x.IdTarea == libre.IdTarea).ExecuteUpdateAsync(s => s.SetProperty(x => x.BloqueaAsignacion, true), Ct);
        }
        Assert.False((await Estado(libre.IdBano)).Disponible); Assert.True((await Estado(libre.IdBano)).MantenimientoBloqueante);
        Assert.Equal("No apto", (await Estado(ocupado.IdBano)).Condicion);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/banos/{libre.IdBano}/responsable-limpieza", new { idEmpleado = a.Id }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/banos/{libre.IdBano}/limpiezas", new ProgramarLimpiezaSolicitud { IdEmpleado = a.Id, IdServicio = ocupado.IdServicio!.Value, IdUbicacion = ocupado.IdUbicacion!.Value, Fecha = new(2030, 4, 4), ClaveProgramacion = Guid.NewGuid() }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync($"/api/banos/{ocupado.IdBano}/limpiezas", await PedidoActual(ocupado, a.Id), Ct)).StatusCode);
        using var final = app.NuevoContexto();
        Assert.Equal(2, (await final.BanosQuimicos.SingleAsync(x => x.IdBano == ocupado.IdBano, Ct)).IdEstadoBano);
        Assert.Equal("Entregado", (await final.Servicios.SingleAsync(x => x.IdServicio == ocupado.IdServicio, Ct)).EstadoServicio);
    }

    [Fact]
    public async Task Servicio_o_ubicacion_desactualizados_y_datos_invalidos_no_crean_tareas()
    {
        var a = await datos.Usuario(); var t = await datos.Tarea(a.Id, null); using var admin = await Admin(); var ruta = $"/api/banos/{t.IdBano}/limpiezas";
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, Pedido(t, a.Id), Ct)).StatusCode); // Ubicación histórica.
        var pedido = await PedidoActual(t, a.Id);
        using (var db = app.NuevoContexto())
        {
            var servicio = await db.Servicios.SingleAsync(x => x.IdServicio == t.IdServicio, Ct);
            servicio.EstadoServicio = "Pendiente de entrega"; servicio.FechaEntregaReal = null; await db.SaveChangesAsync(Ct);
        }
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, pedido, Ct)).StatusCode);
        pedido.ClaveProgramacion = Guid.Empty;
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(ruta, pedido, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(ruta, new { idEmpleado = a.Id, tipo = "Mantenimiento" }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync("/api/banos/2147483647/responsable-limpieza", new { idEmpleado = a.Id }, Ct)).StatusCode);
        using var final = app.NuevoContexto(); Assert.False(await final.TareasOperativas.AnyAsync(x => x.IdBano == t.IdBano && x.IdTipoOperacion == 9, Ct));
    }

    private sealed class Captura : IComponentActivator
    {
        public object? Componente;
        public IComponent CreateInstance(Type tipo) { var c = (IComponent)Activator.CreateInstance(tipo)!; if (tipo.Name == "EstadoBanos") Componente = c; return c; }
    }
    [Fact]
    public async Task Razor_tarjeta_asigna_propone_habitual_y_programa_con_cliente_real()
    {
        var empleado = await datos.Usuario(); var tarea = await datos.Tarea(empleado.Id, null); using var http = datos.Http();
        var sesion = new ClienteApp.SesionUsuario(new ClienteApp.ClienteAutenticacion(http), new AutenticacionAppPruebas.AlmacenPrueba { Token = await datos.Token(app.CorreoAdmin, app.ClaveAdmin) });
        await sesion.InicializarAsync(); var captura = new Captura();
        await using var proveedor = new ServiceCollection().AddLogging().AddSingleton(sesion).AddSingleton<IComponentActivator>(captura).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(proveedor, proveedor.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var vista = await renderer.RenderComponentAsync<global::BanosQuimicos.App.Components.Administracion.EstadoBanos>();
            Assert.Contains(tarea.Bano.CodigoInterno, vista.ToHtmlString()); Assert.Contains("Asignar", vista.ToHtmlString());
            var bano = (await sesion.EstadoBanosAsync()).Single(x => x.IdBano == tarea.IdBano);
            var componente = captura.Componente!; var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            async Task Invocar(string metodo, params object[] args) { var resultado = componente.GetType().GetMethod(metodo, flags)!.Invoke(componente, args); if (resultado is Task t) await t; typeof(ComponentBase).GetMethod("StateHasChanged", flags)!.Invoke(componente, null); }
            await Invocar("Abrir", bano);
            componente.GetType().GetField("empleado", flags)!.SetValue(componente, empleado.Id);
            await Invocar("GuardarResponsableAsync", bano);
            Assert.Contains("Responsable habitual guardado", vista.ToHtmlString());
            var guardado = (await sesion.EstadoBanosAsync()).Single(x => x.IdBano == tarea.IdBano);
            await Invocar("Abrir", guardado); await Invocar("Abrir", guardado);
            Assert.Equal(empleado.Id, componente.GetType().GetField("empleado", flags)!.GetValue(componente));
            componente.GetType().GetField("fecha", flags)!.SetValue(componente, new DateOnly(2030, 4, 4));
            await Invocar("ProgramarAsync", guardado);
            Assert.Contains("Limpieza programada", vista.ToHtmlString());
            await Invocar("ProgramarAsync", guardado); // Botón repetido no genera otro envío.
        });
        using var db = app.NuevoContexto(); Assert.Equal(1, await db.TareasOperativas.CountAsync(x => x.IdBano == tarea.IdBano && x.IdTipoOperacion == 9, Ct));
    }
}
