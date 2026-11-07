using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using BanosQuimicos.Api.Contratos.Solicitudes;
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

public class SolicitudesPruebas(AplicacionTareasPruebas app) : IClassFixture<AplicacionTareasPruebas>
{
    private readonly TareasEmpleadoPruebas datos = new(app);
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private async Task<HttpClient> Admin() => datos.Http(await datos.Token(app.CorreoAdmin, app.ClaveAdmin));
    private async Task<SolicitudCliente> Solicitud(int tipo = 3, int estado = 1, string? modalidad = null)
    {
        var baseTarea = await datos.Tarea(null, null);
        using var db = app.NuevoContexto(); var servicio = await db.Servicios.SingleAsync(x => x.IdServicio == baseTarea.IdServicio, Ct);
        // La tarea auxiliar solo crea el contexto del fixture; en baja no representa otro trabajo pendiente.
        if (tipo == 6)
            await db.TareasOperativas.Where(x => x.IdTarea == baseTarea.IdTarea).ExecuteUpdateAsync(x => x.SetProperty(t => t.IdEstadoOperacion, 9).SetProperty(t => t.FechaCancelacion, AplicacionTareasPruebas.Ahora), Ct);
        var solicitud = new SolicitudCliente { IdCliente = servicio.IdCliente, IdServicioReferencia = tipo == 1 ? null : servicio.IdServicio,
            IdTipoOperacion = tipo, IdEstadoOperacion = estado, FechaSolicitud = AplicacionTareasPruebas.Ahora,
            Descripcion = "PRUEBA SOLICITUD " + Guid.NewGuid().ToString("N"), ModalidadCambioDireccion = modalidad,
            NuevaDireccion = tipo == 4 ? "PRUEBA NUEVO DESTINO" : null, CantidadBanos = tipo is 1 or 2 ? 2 : null,
            IdTipoBanoSolicitado = tipo is 1 or 2 ? 1 : null, DireccionSolicitada = tipo is 1 or 2 ? "PRUEBA DIRECCION PEDIDA" : null };
        db.SolicitudesCliente.Add(solicitud); await db.SaveChangesAsync(Ct); return solicitud;
    }
    private async Task<AsignarSolicitud> Pedido(SolicitudCliente s, int empleado, int? tipo = null, bool bloquea = false)
    {
        using var db = app.NuevoContexto();
        return new() { ClaveProgramacion = Guid.NewGuid(), IdEmpleado = empleado, IdEstadoAnterior = s.IdEstadoOperacion,
            IdServicioEsperado = s.IdServicioReferencia ?? 1,
            IdUbicacionEsperada = s.IdServicioReferencia is null ? 1 : await db.UbicacionesServicio.Where(x => x.IdServicio == s.IdServicioReferencia && x.EsActual).Select(x => x.IdUbicacion).SingleAsync(Ct),
            IdTipoTareaReclamo = tipo, BloqueaAsignacion = bloquea, Fecha = new DateOnly(2030, 4, 4) };
    }

    [Theory]
    [InlineData(3, null, null, false, 3)]
    [InlineData(5, null, 3, false, 3)]
    [InlineData(5, null, 11, true, 11)]
    [InlineData(6, null, null, false, 8)]
    [InlineData(4, "Solicitado", null, false, 10)]
    public async Task Asignar_aprueba_sin_resolver_y_empleado_finaliza_con_efectos_correctos(int tipo, string? modalidad, int? trabajo, bool bloquea, int esperado)
    {
        var habitual = await datos.Usuario(); var puntual = await datos.Usuario(); var s = await Solicitud(tipo, modalidad: modalidad);
        using (var db = app.NuevoContexto())
        {
            var servicio = await db.Servicios.Include(x => x.Bano).SingleAsync(x => x.IdServicio == s.IdServicioReferencia, Ct);
            servicio.Bano.IdResponsableLimpieza = habitual.Id; await db.SaveChangesAsync(Ct);
        }
        using var admin = await Admin(); var pedido = await Pedido(s, puntual.Id, trabajo, bloquea);
        var respuesta = await admin.PostAsJsonAsync($"/api/solicitudes/{s.IdSolicitud}/asignar", pedido, Ct);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var asignada = (await respuesta.Content.ReadFromJsonAsync<SolicitudDetalleRespuesta>(Ct))!;
        Assert.Equal("Aprobada", asignada.Estado); Assert.Null(asignada.FechaResolucion);
        var tareaId = Assert.Single(asignada.Trabajos).IdTarea;
        Assert.Equal("Pendiente", asignada.Trabajos[0].Estado);
        using var empleado = datos.Http(puntual.Token); using var otro = datos.Http(habitual.Token);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"/api/tareas/{tareaId}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.PostAsync($"/api/tareas/{tareaId}/registrar-trabajo", null, Ct)).StatusCode);
        var detalle = (await empleado.GetFromJsonAsync<TareaAsignadaRespuesta>($"/api/tareas/{tareaId}", Ct))!;
        Assert.Equal(s.Descripcion, detalle.DescripcionSolicitud);
        using (var db = app.NuevoContexto())
        {
            var t = await db.TareasOperativas.SingleAsync(x => x.IdTarea == tareaId, Ct);
            Assert.Equal(esperado, t.IdTipoOperacion); Assert.Equal(bloquea, t.BloqueaAsignacion);
            Assert.Null(t.ObservacionesEmpleado); Assert.Null(t.PapelHigienicoRepuesto);
            if (esperado == 10)
            {
                var destino = await db.UbicacionesServicio.SingleAsync(x => x.IdUbicacion == t.IdUbicacionDestino, Ct);
                Assert.False(destino.EsActual); Assert.Null(destino.FechaDesde);
            }
        }
        Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{tareaId}/registrar-trabajo", null, Ct)).StatusCode);
        foreach (var _ in Enumerable.Range(0, 2)) Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{tareaId}/finalizar", null, Ct)).StatusCode);
        var resuelta = (await admin.GetFromJsonAsync<SolicitudDetalleRespuesta>($"/api/solicitudes/{s.IdSolicitud}", Ct))!;
        Assert.Equal("Resuelta", resuelta.Estado); Assert.Equal(AplicacionTareasPruebas.Ahora, resuelta.FechaResolucion);
        using var final = app.NuevoContexto(); var serv = await final.Servicios.Include(x => x.Bano).SingleAsync(x => x.IdServicio == s.IdServicioReferencia, Ct);
        Assert.Equal(habitual.Id, serv.Bano.IdResponsableLimpieza); Assert.Equal(1, serv.Bano.IdEstadoBano);
        Assert.Equal(esperado == 8 ? "Finalizado" : "Entregado", serv.EstadoServicio);
        if (esperado == 10) { Assert.Equal("PRUEBA NUEVO DESTINO", resuelta.Direccion); Assert.NotNull(resuelta.FechaCambioEfectiva); Assert.Equal("PRUEBA OTRA DIRECCION ACTUAL", detalle.Direccion); }
    }

    [Fact]
    public async Task Reintentos_y_asignaciones_concurrentes_no_crean_duplicados()
    {
        var empleado = await datos.Usuario(); var s = await Solicitud(4, modalidad: "Solicitado"); using var admin = await Admin();
        var pedido = await Pedido(s, empleado.Id); var ruta = $"/api/solicitudes/{s.IdSolicitud}/asignar";
        var respuestas = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => admin.PostAsJsonAsync(ruta, pedido, Ct)));
        Assert.All(respuestas, x => Assert.Equal(HttpStatusCode.OK, x.StatusCode));
        foreach (var r in respuestas) r.Dispose();
        pedido.ClaveProgramacion = Guid.NewGuid(); Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, pedido, Ct)).StatusCode);
        using var db = app.NuevoContexto();
        Assert.Equal(1, await db.TareasOperativas.CountAsync(x => x.IdSolicitud == s.IdSolicitud, Ct));
        Assert.Equal(3, await db.UbicacionesServicio.CountAsync(x => x.IdServicio == s.IdServicioReferencia, Ct));
        var original = await db.TareasOperativas.SingleAsync(x => x.IdSolicitud == s.IdSolicitud, Ct);
        pedido.ClaveProgramacion = original.ClaveProgramacion!.Value; pedido.Fecha = pedido.Fecha.AddDays(1);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, pedido, Ct)).StatusCode);
    }

    [Theory]
    [InlineData(1, null)]
    [InlineData(2, null)]
    [InlineData(4, "Informado")]
    public async Task Otros_tratamientos_no_se_aprueban_ni_resuelven_por_asignar(int tipo, string? modalidad)
    {
        var e = await datos.Usuario(); var s = await Solicitud(tipo, modalidad: modalidad); using var admin = await Admin();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/solicitudes/{s.IdSolicitud}/asignar", await Pedido(s, e.Id), Ct)).StatusCode);
        using var db = app.NuevoContexto(); Assert.Equal(1, (await db.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == s.IdSolicitud, Ct)).IdEstadoOperacion);
        Assert.False(await db.TareasOperativas.AnyAsync(x => x.IdSolicitud == s.IdSolicitud, Ct));
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await admin.DeleteAsync($"/api/solicitudes/{s.IdSolicitud}", Ct)).StatusCode);
    }

    [Theory]
    [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public async Task Estado_terminal_rechaza_asignacion_sin_alterarlo(int estado)
    {
        var e = await datos.Usuario(); var s = await Solicitud(3, estado); using var admin = await Admin(); var p = await Pedido(s, e.Id); p.IdEstadoAnterior = 1;
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/solicitudes/{s.IdSolicitud}/asignar", p, Ct)).StatusCode);
        using var db = app.NuevoContexto(); Assert.Equal(estado, (await db.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == s.IdSolicitud, Ct)).IdEstadoOperacion);
    }

    [Theory]
    [InlineData("Anonimo", 401)] [InlineData("Cliente", 403)] [InlineData("Empleado", 403)]
    [InlineData("Pendiente", 403)] [InlineData("Suspendida", 401)] [InlineData("BajaLogica", 401)]
    public async Task Permisos_de_lectura_y_asignacion(string caso, int codigo)
    {
        string? token = null;
        if (caso != "Anonimo")
        {
            var u = await datos.Usuario(caso == "Cliente" ? "Cliente" : "Empleado", caso != "Pendiente"); token = u.Token;
            if (caso is "Suspendida" or "BajaLogica") { using var db = app.NuevoContexto(); await db.Usuarios.Where(x => x.IdUsuario == u.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.EstadoCuenta, caso), Ct); }
        }
        using var http = datos.Http(token);
        foreach (var ruta in new[] { "/api/solicitudes", "/api/solicitudes/1" }) Assert.Equal((HttpStatusCode)codigo, (await http.GetAsync(ruta, Ct)).StatusCode);
        Assert.Equal((HttpStatusCode)codigo, (await http.PostAsJsonAsync("/api/solicitudes/1/asignar", new AsignarSolicitud { IdEmpleado = 1, IdEstadoAnterior = 1, IdServicioEsperado = 1, IdUbicacionEsperada = 1, ClaveProgramacion = Guid.NewGuid(), Fecha = new(2030, 4, 4) }, Ct)).StatusCode);
    }

    [Fact]
    public async Task Validaciones_rechazan_empleado_inactivo_bloqueo_ajeno_contexto_y_datos_extra()
    {
        var e = await datos.Usuario(); var s = await Solicitud(); using var admin = await Admin(); var ruta = $"/api/solicitudes/{s.IdSolicitud}/asignar";
        var p = await Pedido(s, e.Id); p.BloqueaAsignacion = true;
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
        p.BloqueaAsignacion = false; p.IdUbicacionEsperada = int.MaxValue;
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
        p = await Pedido(s, e.Id);
        using (var db = app.NuevoContexto()) { await db.Usuarios.Where(x => x.IdUsuario == e.Id).ExecuteUpdateAsync(x => x.SetProperty(u => u.EstadoCuenta, "Suspendida"), Ct); }
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(ruta, new { idEmpleado = e.Id, estado = "Resuelta" }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync("/api/solicitudes/2147483647/asignar", p, Ct)).StatusCode);
        using var final = app.NuevoContexto(); Assert.False(await final.TareasOperativas.AnyAsync(x => x.IdSolicitud == s.IdSolicitud, Ct));
        Assert.Equal(1, (await final.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == s.IdSolicitud, Ct)).IdEstadoOperacion);
    }

    [Fact]
    public async Task Listado_detalle_y_vacio_no_exponen_credenciales_y_ordenan_establemente()
    {
        var a = await Solicitud(1); var b = await Solicitud(5); using var admin = await Admin();
        var lista = (await admin.GetFromJsonAsync<List<SolicitudResumenRespuesta>>("/api/solicitudes", Ct))!;
        Assert.True(lista.FindIndex(x => x.IdSolicitud == b.IdSolicitud) < lista.FindIndex(x => x.IdSolicitud == a.IdSolicitud));
        var detalle = (await admin.GetFromJsonAsync<SolicitudDetalleRespuesta>($"/api/solicitudes/{a.IdSolicitud}", Ct))!;
        Assert.Null(detalle.CodigoBano); Assert.Equal(2, detalle.CantidadBanos); Assert.Empty(detalle.Trabajos);
        var json = await admin.GetStringAsync($"/api/solicitudes/{b.IdSolicitud}", Ct);
        foreach (var palabra in new[] { "hash", "accessToken", "contrasena" }) Assert.DoesNotContain(palabra, json, StringComparison.OrdinalIgnoreCase);
        await using var vacia = new AplicacionTareasPruebas(); var d = new TareasEmpleadoPruebas(vacia);
        using var http = d.Http(await d.Token(vacia.CorreoAdmin, vacia.ClaveAdmin));
        Assert.Empty((await http.GetFromJsonAsync<List<SolicitudResumenRespuesta>>("/api/solicitudes", Ct))!);
        var sesionVacia = new ClienteApp.SesionUsuario(new ClienteApp.ClienteAutenticacion(vacia.CreateClient()),
            new AutenticacionAppPruebas.AlmacenPrueba { Token = await d.Token(vacia.CorreoAdmin, vacia.ClaveAdmin) });
        await sesionVacia.InicializarAsync();
        await using var sp = new ServiceCollection().AddLogging().AddSingleton(sesionVacia).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(sp, sp.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () => Assert.Contains("No hay solicitudes ni reclamos registrados", (await renderer.RenderComponentAsync<global::BanosQuimicos.App.Components.Administracion.SolicitudesReclamos>()).ToHtmlString()));
    }

    [Fact]
    public async Task Dos_asignaciones_distintas_concurrentes_no_pisan_estado_ni_empleado()
    {
        var a = await datos.Usuario(); var b = await datos.Usuario(); var s = await Solicitud(); using var admin = await Admin();
        var ruta = $"/api/solicitudes/{s.IdSolicitud}/asignar";
        var respuestas = await Task.WhenAll(admin.PostAsJsonAsync(ruta, await Pedido(s, a.Id), Ct), admin.PostAsJsonAsync(ruta, await Pedido(s, b.Id), Ct));
        Assert.Single(respuestas, x => x.StatusCode == HttpStatusCode.OK); Assert.Single(respuestas, x => x.StatusCode == HttpStatusCode.Conflict);
        foreach (var r in respuestas) r.Dispose();
        using var db = app.NuevoContexto(); Assert.Equal(1, await db.TareasOperativas.CountAsync(x => x.IdSolicitud == s.IdSolicitud, Ct));
        Assert.Equal(2, (await db.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == s.IdSolicitud, Ct)).IdEstadoOperacion);
    }

    [Fact]
    public async Task Traslado_sin_destino_no_aprueba_ni_crea_ubicaciones()
    {
        var e = await datos.Usuario(); var s = await Solicitud(4, modalidad: "Solicitado"); using var admin = await Admin();
        using (var db = app.NuevoContexto()) { await db.SolicitudesCliente.Where(x => x.IdSolicitud == s.IdSolicitud).ExecuteUpdateAsync(x => x.SetProperty(s => s.NuevaDireccion, (string?)null), Ct); }
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/solicitudes/{s.IdSolicitud}/asignar", await Pedido(s, e.Id), Ct)).StatusCode);
        using var final = app.NuevoContexto(); Assert.Equal(2, await final.UbicacionesServicio.CountAsync(x => x.IdServicio == s.IdServicioReferencia, Ct));
        Assert.False(await final.TareasOperativas.AnyAsync(x => x.IdSolicitud == s.IdSolicitud, Ct));
        Assert.Equal(1, (await final.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == s.IdSolicitud, Ct)).IdEstadoOperacion);
    }

    [Fact]
    public async Task Servicio_de_otro_cliente_no_permite_asignacion()
    {
        var e = await datos.Usuario(); var s = await Solicitud(); var otro = await datos.Usuario("Cliente"); using var admin = await Admin();
        using (var db = app.NuevoContexto())
        {
            var idCliente = await db.Clientes.Where(x => x.IdUsuario == otro.Id).Select(x => x.IdCliente).SingleAsync(Ct);
            await db.SolicitudesCliente.Where(x => x.IdSolicitud == s.IdSolicitud).ExecuteUpdateAsync(x => x.SetProperty(s => s.IdCliente, idCliente), Ct);
        }
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/solicitudes/{s.IdSolicitud}/asignar", await Pedido(s, e.Id), Ct)).StatusCode);
        using var final = app.NuevoContexto(); Assert.False(await final.TareasOperativas.AnyAsync(x => x.IdSolicitud == s.IdSolicitud, Ct));
    }

    [Fact]
    public async Task Trabajo_cancelado_no_se_ignora_para_resolver_ni_reasignar()
    {
        var e = await datos.Usuario(); var s = await Solicitud(); using var admin = await Admin();
        var r = await admin.PostAsJsonAsync($"/api/solicitudes/{s.IdSolicitud}/asignar", await Pedido(s, e.Id), Ct);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var idTarea = Assert.Single((await r.Content.ReadFromJsonAsync<SolicitudDetalleRespuesta>(Ct))!.Trabajos).IdTarea;
        using (var db = app.NuevoContexto())
        {
            var t = await db.TareasOperativas.SingleAsync(x => x.IdTarea == idTarea, Ct);
            db.TareasOperativas.Add(new TareaOperativa { IdSolicitud = s.IdSolicitud, IdServicio = t.IdServicio,
                IdBano = t.IdBano, IdEmpleado = e.Id, IdTipoOperacion = 3, IdEstadoOperacion = 9, FechaRegistro = AplicacionTareasPruebas.Ahora });
            await db.SaveChangesAsync(Ct);
        }
        using var trabajador = datos.Http(e.Token);
        foreach (var accion in new[] { "registrar-trabajo", "finalizar" }) Assert.Equal(HttpStatusCode.OK, (await trabajador.PostAsync($"/api/tareas/{idTarea}/" + accion, null, Ct)).StatusCode);
        var actual = (await admin.GetFromJsonAsync<SolicitudDetalleRespuesta>($"/api/solicitudes/{s.IdSolicitud}", Ct))!;
        Assert.Equal("Aprobada", actual.Estado); Assert.Null(actual.FechaResolucion); Assert.Equal(2, actual.Trabajos.Count);
        var pedido = await Pedido(s, e.Id); pedido.IdEstadoAnterior = 2;
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/solicitudes/{s.IdSolicitud}/asignar", pedido, Ct)).StatusCode);
    }

    private sealed class Captura : IComponentActivator
    {
        public object? Vista;
        public IComponent CreateInstance(Type tipo) { var c = (IComponent)Activator.CreateInstance(tipo)!; if (tipo.Name == "SolicitudesReclamos") Vista = c; return c; }
    }
    [Fact]
    public async Task Razor_seleccionar_no_guarda_confirmar_asigna_y_consultar_refleja_resolucion()
    {
        var e = await datos.Usuario(); var s = await Solicitud(5); using var http = datos.Http();
        var sesion = new ClienteApp.SesionUsuario(new ClienteApp.ClienteAutenticacion(http), new AutenticacionAppPruebas.AlmacenPrueba { Token = await datos.Token(app.CorreoAdmin, app.ClaveAdmin) });
        await sesion.InicializarAsync(); var captura = new Captura();
        await using var sp = new ServiceCollection().AddLogging().AddSingleton(sesion).AddSingleton<IComponentActivator>(captura).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(sp, sp.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var vista = await renderer.RenderComponentAsync<global::BanosQuimicos.App.Components.Administracion.SolicitudesReclamos>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic; var c = captura.Vista!;
            async Task Invocar(string metodo, params object[] args) { var v = c.GetType().GetMethod(metodo, flags)!.Invoke(c, args); if (v is Task t) await t; typeof(ComponentBase).GetMethod("StateHasChanged", flags)!.Invoke(c, null); }
            void Campo(string nombre, object? valor) => c.GetType().GetField(nombre, flags)!.SetValue(c, valor);
            await Invocar("AbrirAsync", s.IdSolicitud);
            Assert.Contains(s.Descripcion!, vista.ToHtmlString()); Campo("seleccion", true); Campo("empleado", e.Id); Campo("tipoReclamo", 11);
            using (var db = app.NuevoContexto()) Assert.False(await db.TareasOperativas.AnyAsync(x => x.IdSolicitud == s.IdSolicitud, Ct));
            await Invocar("Cancelar"); Assert.DoesNotContain("Selecci&#xF3;n sin guardar", vista.ToHtmlString());
            Campo("seleccion", true); Campo("empleado", e.Id); Campo("tipoReclamo", 11); Campo("bloquea", true); Campo("fecha", new DateOnly(2030, 4, 4));
            await Invocar("GuardarAsync"); Assert.Contains("Asignaci&#xF3;n guardada", vista.ToHtmlString()); Assert.Contains("Aprobada", vista.ToHtmlString());
            var detalle = await sesion.DetalleSolicitudAsync(s.IdSolicitud); var id = Assert.Single(detalle.Trabajos).IdTarea;
            using var trabajador = datos.Http(e.Token);
            foreach (var accion in new[] { "registrar-trabajo", "finalizar" }) Assert.Equal(HttpStatusCode.OK, (await trabajador.PostAsync($"/api/tareas/{id}/" + accion, null, Ct)).StatusCode);
            await Invocar("AbrirAsync", s.IdSolicitud); Assert.Contains("Resuelta", vista.ToHtmlString());
            Assert.DoesNotContain("Aprobar y asignar", vista.ToHtmlString()); await Invocar("CargarAsync"); Assert.Contains("Solicitudes y Reclamos", vista.ToHtmlString());
        });
    }
}
