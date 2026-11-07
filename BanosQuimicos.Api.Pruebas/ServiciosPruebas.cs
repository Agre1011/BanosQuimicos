using System.Net;
using System.Net.Http.Json;
using BanosQuimicos.Api.Contratos.Servicios;
using BanosQuimicos.Api.Contratos.Solicitudes;
using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanosQuimicos.Api.Pruebas;

public class ServiciosPruebas(AplicacionTareasPruebas app) : IClassFixture<AplicacionTareasPruebas>
{
    private readonly TareasEmpleadoPruebas datos = new(app);
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private static DateTimeOffset Ahora => AplicacionTareasPruebas.Ahora;
    private record Caso(Servicio Servicio, int Empleado, string Token, int Ubicacion, int? Solicitud, int? Limpieza, int? Mantenimiento);
    private async Task<HttpClient> Admin() => datos.Http(await datos.Token(app.CorreoAdmin, app.ClaveAdmin));
    private async Task<Caso> Preparar(bool baja = false, bool limpieza = false, bool bloqueo = false, int condicion = 1)
    {
        var e = await datos.Usuario(); var cliente = await datos.Usuario("Cliente"); using var db = app.NuevoContexto();
        var c = await db.Clientes.Where(x => x.IdUsuario == cliente.Id).Select(x => x.IdCliente).SingleAsync(Ct);
        var s = new Servicio { IdCliente = c, Bano = new BanoQuimico { CodigoInterno = "RT-" + Guid.NewGuid().ToString("N")[..12], IdTipoBano = 1, IdEstadoBano = condicion, FechaAlta = Ahora.AddDays(-11) },
            EstadoServicio = "Entregado", TipoUso = "Obra", CantidadLimpiezasSemana = 1, FechaAsignacion = Ahora.AddDays(-10), FechaEntregaReal = Ahora.AddDays(-9) };
        var u = new UbicacionServicio { Servicio = s, Direccion = "PRUEBA RETIRO LOCAL 123", FechaRegistro = Ahora.AddDays(-9), FechaDesde = Ahora.AddDays(-9), EsActual = true };
        db.UbicacionesServicio.Add(u); await db.SaveChangesAsync(Ct);
        SolicitudCliente? solicitud = null;
        if (baja) { solicitud = new SolicitudCliente { IdCliente = c, IdServicioReferencia = s.IdServicio, IdTipoOperacion = 6, IdEstadoOperacion = 1, FechaSolicitud = Ahora.AddDays(-1), Descripcion = "PRUEBA BAJA" }; db.SolicitudesCliente.Add(solicitud); }
        TareaOperativa? l = null, m = null;
        if (limpieza)
        {
            l = new TareaOperativa { Servicio = s, IdBano = s.IdBano, IdEmpleado = e.Id, IdUbicacion = u.IdUbicacion, IdTipoOperacion = 9, IdEstadoOperacion = 6, FechaRegistro = Ahora, FechaProgramada = Ahora.AddDays(7),
                Solicitud = new SolicitudCliente { IdCliente = c, IdServicioReferencia = s.IdServicio, IdTipoOperacion = 3, IdEstadoOperacion = 2, FechaSolicitud = Ahora, Descripcion = "PRUEBA TRABAJO A REVISAR" } };
            db.TareasOperativas.Add(l);
        }
        if (bloqueo) { m = new TareaOperativa { Servicio = s, IdBano = s.IdBano, IdEmpleado = e.Id, IdUbicacion = u.IdUbicacion, IdTipoOperacion = 11, IdEstadoOperacion = 6, BloqueaAsignacion = true, FechaRegistro = Ahora }; db.TareasOperativas.Add(m); }
        await db.SaveChangesAsync(Ct);
        return new(s, e.Id, e.Token, u.IdUbicacion, solicitud?.IdSolicitud, l?.IdTarea, m?.IdTarea);
    }
    private static ProgramarRetiroSolicitud Pedido(Caso c) => new() { ClaveProgramacion = Guid.NewGuid(), IdEmpleado = c.Empleado,
        IdUbicacionEsperada = c.Ubicacion, IdSolicitudBaja = c.Solicitud, IdEstadoSolicitudAnterior = c.Solicitud is null ? null : 1, Fecha = new(2030, 4, 4) };
    private async Task<int> Programar(HttpClient admin, Caso c, ProgramarRetiroSolicitud? pedido = null)
    {
        var r = await admin.PostAsJsonAsync($"/api/servicios/{c.Servicio.IdServicio}/retiro", pedido ?? Pedido(c), Ct);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return Assert.Single((await r.Content.ReadFromJsonAsync<ServicioGestionRespuesta>(Ct))!.Trabajos, x => x.IdTipoOperacion == 8).IdTarea;
    }
    private async Task Registrar(HttpClient empleado, int id) => Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{id}/registrar-trabajo", null, Ct)).StatusCode);
    private async Task Finalizar(HttpClient empleado, int id) => Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{id}/finalizar", null, Ct)).StatusCode);

    [Fact]
    public async Task Retiro_vincula_baja_sin_liberar_hasta_cierre_y_conserva_responsable_e_historial()
    {
        var c = await Preparar(baja: true); var habitual = await datos.Usuario(); using var admin = await Admin();
        using (var db = app.NuevoContexto()) { await db.BanosQuimicos.Where(x => x.IdBano == c.Servicio.IdBano).ExecuteUpdateAsync(x => x.SetProperty(b => b.IdResponsableLimpieza, habitual.Id), Ct); }
        var p = Pedido(c); var id = await Programar(admin, c, p);
        var previo = (await admin.GetFromJsonAsync<ServicioGestionRespuesta>($"/api/servicios/{c.Servicio.IdServicio}", Ct))!;
        Assert.Equal("Entregado", previo.Estado); Assert.False(previo.DisponibleAhora); Assert.Null(previo.FechaRetiroReal); Assert.NotNull(previo.ResponsableHabitual);
        Assert.Equal("Aprobada", Assert.Single(previo.SolicitudesBaja).Estado);
        using var ajeno = datos.Http(habitual.Token); Assert.Equal(HttpStatusCode.NotFound, (await ajeno.PostAsync($"/api/tareas/{id}/registrar-trabajo", null, Ct)).StatusCode);
        using var e = datos.Http(c.Token); await Registrar(e, id); await Finalizar(e, id); await Finalizar(e, id);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/servicios/{c.Servicio.IdServicio}/retiro", p, Ct)).StatusCode);
        var final = (await admin.GetFromJsonAsync<ServicioGestionRespuesta>($"/api/servicios/{c.Servicio.IdServicio}", Ct))!;
        Assert.Equal("Finalizado", final.Estado); Assert.True(final.DisponibleAhora); Assert.Equal(Ahora, final.FechaRetiroReal); Assert.Equal(Ahora, final.FechaFinalizacion);
        Assert.Equal("Resuelta", Assert.Single(final.SolicitudesBaja).Estado); Assert.Equal(previo.ResponsableHabitual, final.ResponsableHabitual);
        using var contexto = app.NuevoContexto(); Assert.True(await contexto.Servicios.AnyAsync(x => x.IdServicio == c.Servicio.IdServicio, Ct));
        Assert.Equal(1, await contexto.TareasOperativas.CountAsync(x => x.IdServicio == c.Servicio.IdServicio && x.IdTipoOperacion == 8, Ct));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/servicios/{c.Servicio.IdServicio}/retiro", Pedido(c), Ct)).StatusCode);
    }

    [Fact]
    public async Task Cancelar_limpieza_futura_es_explicito_idempotente_y_no_resuelve_su_solicitud()
    {
        var c = await Preparar(limpieza: true); using var admin = await Admin(); var id = await Programar(admin, c); using var e = datos.Http(c.Token);
        await Registrar(e, id); Assert.Equal(HttpStatusCode.Conflict, (await e.PostAsync($"/api/tareas/{id}/finalizar", null, Ct)).StatusCode);
        var decision = new ResolverPendientesSolicitud { IdCancelar = [c.Limpieza!.Value] };
        var ruta = $"/api/servicios/{c.Servicio.IdServicio}/resolver-pendientes";
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync(ruta, decision, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync(ruta, decision, Ct)).StatusCode);
        await Finalizar(e, id);
        using var db = app.NuevoContexto(); var t = await db.TareasOperativas.SingleAsync(x => x.IdTarea == c.Limpieza, Ct);
        Assert.Equal(9, t.IdEstadoOperacion); Assert.Equal(Ahora, t.FechaCancelacion); Assert.Equal(c.Empleado, t.IdEmpleado);
        Assert.Equal(2, (await db.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == t.IdSolicitud, Ct)).IdEstadoOperacion);
        Assert.Equal(1, await db.SolicitudesCliente.CountAsync(x => x.IdServicioReferencia == c.Servicio.IdServicio, Ct)); // No solicitud de baja inventada.
    }

    [Theory]
    [InlineData(7, false)] [InlineData(6, true)]
    public async Task En_curso_o_cumplimiento_registrado_no_se_cancela_y_debe_completarse(int estado, bool registrado)
    {
        var c = await Preparar(limpieza: true); using var admin = await Admin(); var retiro = await Programar(admin, c);
        using (var db = app.NuevoContexto())
        {
            var t = await db.TareasOperativas.SingleAsync(x => x.IdTarea == c.Limpieza, Ct); t.IdEstadoOperacion = estado;
            if (registrado) { t.FechaTrabajoRegistrado = Ahora; t.IdEmpleadoRegistroTrabajo = c.Empleado; }
            await db.SaveChangesAsync(Ct);
        }
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/servicios/{c.Servicio.IdServicio}/resolver-pendientes", new ResolverPendientesSolicitud { IdCancelar = [c.Limpieza!.Value] }, Ct)).StatusCode);
        using var e = datos.Http(c.Token); await Registrar(e, retiro);
        Assert.Equal(HttpStatusCode.Conflict, (await e.PostAsync($"/api/tareas/{retiro}/finalizar", null, Ct)).StatusCode);
        await Registrar(e, c.Limpieza.Value); await Finalizar(e, c.Limpieza.Value); await Finalizar(e, retiro);
    }

    [Theory]
    [InlineData(1, true)] [InlineData(2, false)]
    public async Task Mantenimiento_bloqueante_sigue_despues_del_retiro_y_no_cambia_condicion(int condicion, bool disponibleTrasReparar)
    {
        var c = await Preparar(bloqueo: true, condicion: condicion); using var admin = await Admin(); var retiro = await Programar(admin, c);
        var ruta = $"/api/servicios/{c.Servicio.IdServicio}/resolver-pendientes";
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, new ResolverPendientesSolicitud { IdCancelar = [c.Mantenimiento!.Value] }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync(ruta, new ResolverPendientesSolicitud { IdConservar = [c.Mantenimiento!.Value] }, Ct)).StatusCode);
        using var e = datos.Http(c.Token); await Registrar(e, retiro); await Finalizar(e, retiro);
        var cerrado = (await admin.GetFromJsonAsync<ServicioGestionRespuesta>($"/api/servicios/{c.Servicio.IdServicio}", Ct))!;
        Assert.Equal("Finalizado", cerrado.Estado); Assert.True(cerrado.MantenimientoBloqueante); Assert.False(cerrado.DisponibleAhora);
        Assert.Equal("Pendiente", Assert.Single(cerrado.Trabajos, x => x.IdTarea == c.Mantenimiento).Estado);
        await Registrar(e, c.Mantenimiento.Value); await Finalizar(e, c.Mantenimiento.Value);
        var reparado = (await admin.GetFromJsonAsync<ServicioGestionRespuesta>($"/api/servicios/{c.Servicio.IdServicio}", Ct))!;
        Assert.Equal(disponibleTrasReparar, reparado.DisponibleAhora); Assert.False(reparado.MantenimientoBloqueante);
        Assert.Equal(cerrado.CondicionBano, reparado.CondicionBano);
    }

    [Fact]
    public async Task Concurrencia_y_rutas_distintas_no_duplican_un_retiro()
    {
        var c = await Preparar(); using var admin = await Admin(); var p = Pedido(c); var ruta = $"/api/servicios/{c.Servicio.IdServicio}/retiro";
        var respuestas = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => admin.PostAsJsonAsync(ruta, p, Ct)));
        Assert.All(respuestas, x => Assert.Equal(HttpStatusCode.OK, x.StatusCode)); foreach (var r in respuestas) r.Dispose();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, Pedido(c), Ct)).StatusCode);
        int solicitud;
        using (var db = app.NuevoContexto())
        {
            var s = new SolicitudCliente { IdServicioReferencia = c.Servicio.IdServicio, IdCliente = c.Servicio.IdCliente, IdTipoOperacion = 6, IdEstadoOperacion = 1, FechaSolicitud = Ahora }; db.SolicitudesCliente.Add(s); await db.SaveChangesAsync(Ct); solicitud = s.IdSolicitud;
        }
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/solicitudes/{solicitud}/asignar", new AsignarSolicitud { ClaveProgramacion = Guid.NewGuid(), IdEmpleado = c.Empleado, IdEstadoAnterior = 1,
            IdServicioEsperado = c.Servicio.IdServicio, IdUbicacionEsperada = c.Ubicacion, Fecha = new(2030, 4, 4) }, Ct)).StatusCode);
        using var final = app.NuevoContexto(); Assert.Equal(1, await final.TareasOperativas.CountAsync(x => x.IdServicio == c.Servicio.IdServicio && x.IdTipoOperacion == 8, Ct));
        Assert.Equal(1, (await final.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == solicitud, Ct)).IdEstadoOperacion);
    }

    [Theory]
    [InlineData("Anonimo", 401)] [InlineData("Cliente", 403)] [InlineData("Empleado", 403)]
    public async Task Listado_programacion_y_decisiones_son_exclusivos_del_administrador(string rol, int codigo)
    {
        var c = await Preparar(); string? token = rol == "Anonimo" ? null : (await datos.Usuario(rol)).Token; using var http = datos.Http(token);
        foreach (var ruta in new[] { "/api/servicios", $"/api/servicios/{c.Servicio.IdServicio}" }) Assert.Equal((HttpStatusCode)codigo, (await http.GetAsync(ruta, Ct)).StatusCode);
        Assert.Equal((HttpStatusCode)codigo, (await http.PostAsJsonAsync($"/api/servicios/{c.Servicio.IdServicio}/retiro", Pedido(c), Ct)).StatusCode);
        Assert.Equal((HttpStatusCode)codigo, (await http.PostAsJsonAsync($"/api/servicios/{c.Servicio.IdServicio}/resolver-pendientes", new ResolverPendientesSolicitud(), Ct)).StatusCode);
    }

    [Fact]
    public async Task Validaciones_impiden_empleado_invalido_ubicacion_obsoleta_y_retiro_antes_de_entregar()
    {
        var c = await Preparar(); var cliente = await datos.Usuario("Cliente"); using var admin = await Admin(); var p = Pedido(c); var ruta = $"/api/servicios/{c.Servicio.IdServicio}/retiro";
        p.IdEmpleado = cliente.Id; Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
        p.IdEmpleado = c.Empleado; p.IdUbicacionEsperada = int.MaxValue; Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
        p.IdUbicacionEsperada = c.Ubicacion;
        using (var db = app.NuevoContexto()) { var s = await db.Servicios.SingleAsync(x => x.IdServicio == c.Servicio.IdServicio, Ct); s.EstadoServicio = "Pendiente de entrega"; s.FechaEntregaReal = null; await db.SaveChangesAsync(Ct); }
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync("/api/servicios/2147483647/retiro", p, Ct)).StatusCode);
        using var final = app.NuevoContexto(); Assert.False(await final.TareasOperativas.AnyAsync(x => x.IdServicio == c.Servicio.IdServicio, Ct));
    }

    [Fact]
    public async Task Varias_solicitudes_exigen_eleccion_y_solo_se_resuelve_la_vinculada()
    {
        var c = await Preparar(baja: true); using var admin = await Admin(); int segunda;
        using (var db = app.NuevoContexto()) { var s = new SolicitudCliente { IdCliente = c.Servicio.IdCliente, IdServicioReferencia = c.Servicio.IdServicio, IdTipoOperacion = 6, IdEstadoOperacion = 1, FechaSolicitud = Ahora }; db.SolicitudesCliente.Add(s); await db.SaveChangesAsync(Ct); segunda = s.IdSolicitud; }
        var p = Pedido(c); p.IdSolicitudBaja = null; p.IdEstadoSolicitudAnterior = null;
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/servicios/{c.Servicio.IdServicio}/retiro", p, Ct)).StatusCode);
        p.IdSolicitudBaja = segunda; p.IdEstadoSolicitudAnterior = 1; var retiro = await Programar(admin, c, p); using var e = datos.Http(c.Token); await Registrar(e, retiro); await Finalizar(e, retiro);
        using var final = app.NuevoContexto(); Assert.Equal(4, (await final.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == segunda, Ct)).IdEstadoOperacion);
        Assert.Equal(1, (await final.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == c.Solicitud, Ct)).IdEstadoOperacion);
    }

    [Fact]
    public async Task Revision_desactualizada_no_cancela_silenciosamente_trabajos_nuevos()
    {
        var c = await Preparar(limpieza: true); using var admin = await Admin(); await Programar(admin, c);
        using (var db = app.NuevoContexto()) { db.TareasOperativas.Add(new TareaOperativa { IdServicio = c.Servicio.IdServicio, IdBano = c.Servicio.IdBano, IdTipoOperacion = 9, IdEstadoOperacion = 6, FechaRegistro = Ahora, IdEmpleado = c.Empleado }); await db.SaveChangesAsync(Ct); }
        var r = await admin.PostAsJsonAsync($"/api/servicios/{c.Servicio.IdServicio}/resolver-pendientes", new ResolverPendientesSolicitud { IdCancelar = [c.Limpieza!.Value] }, Ct);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        using var final = app.NuevoContexto(); Assert.Equal(6, (await final.TareasOperativas.SingleAsync(x => x.IdTarea == c.Limpieza, Ct)).IdEstadoOperacion);
    }
}
