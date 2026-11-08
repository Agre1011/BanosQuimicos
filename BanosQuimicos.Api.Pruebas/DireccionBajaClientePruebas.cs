using System.Net;
using System.Net.Http.Json;
using BanosQuimicos.Api.Contratos.Solicitudes;
using BanosQuimicos.Api.Contratos.Servicios;
using BanosQuimicos.Api.Controllers;
using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanosQuimicos.Api.Pruebas;

public class DireccionBajaClientePruebas(AplicacionTareasPruebas app) : IClassFixture<AplicacionTareasPruebas>
{
    private readonly TareasEmpleadoPruebas datos = new(app);
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private static DateTimeOffset Ahora => AplicacionTareasPruebas.Ahora;
    private record Caso(int Servicio, int Cliente, string Token, int Empleado, string TokenEmpleado, int Origen, int Bano);
    private static string Direccion(int servicio) => $"/api/solicitudes-cliente/servicios/{servicio}/cambio-direccion";
    private static string Baja(int servicio) => $"/api/solicitudes-cliente/servicios/{servicio}/baja";
    private static string Revision(int id) => $"/api/solicitudes/{id}/direccion";
    private async Task<HttpClient> Admin() => datos.Http(await datos.Token(app.CorreoAdmin, app.ClaveAdmin));
    private async Task<Caso> Preparar()
    {
        var c = await datos.Usuario("Cliente"); var e = await datos.Usuario(); using var db = app.NuevoContexto();
        var s = new Servicio { IdCliente = await db.Clientes.Where(c1 => c1.IdUsuario == c.Id).Select(c1 => c1.IdCliente).SingleAsync(Ct),
            Bano = new BanoQuimico { CodigoInterno = "DB-" + Guid.NewGuid().ToString("N")[..12], IdTipoBano = 1, IdEstadoBano = 1,
                IdResponsableLimpieza = e.Id, FechaAlta = Ahora.AddDays(-5) }, TipoUso = "Obra", EstadoServicio = "Entregado",
            FechaAsignacion = Ahora.AddDays(-4), FechaEntregaReal = Ahora.AddDays(-3) };
        var u = new UbicacionServicio { Servicio = s, Direccion = "PRUEBA ORIGEN 123", EsActual = true, FechaRegistro = Ahora.AddDays(-3), FechaDesde = Ahora.AddDays(-3) };
        db.UbicacionesServicio.Add(u); await db.SaveChangesAsync(Ct); return new(s.IdServicio, c.Id, c.Token, e.Id, e.Token, u.IdUbicacion, s.IdBano);
    }
    private static CambioDireccionSolicitud Cambio(Caso c, string modalidad = "Informado") => new() {
        ClaveEnvio = Guid.NewGuid(), IdUbicacionEsperada = c.Origen, NuevaDireccion = "PRUEBA NUEVO DESTINO 456", Modalidad = modalidad,
        FechaEfectiva = modalidad == "Informado" ? Ahora.AddHours(-1) : null };
    private async Task<TareaOperativa> Tarea(Caso c, int estado = 6, bool registrada = false)
    {
        using var db = app.NuevoContexto(); var t = new TareaOperativa { IdServicio = c.Servicio, IdBano = c.Bano, IdEmpleado = c.Empleado,
            IdUbicacion = c.Origen, IdTipoOperacion = 9, IdEstadoOperacion = estado, FechaRegistro = Ahora.AddDays(-2),
            FechaTrabajoRegistrado = registrada ? Ahora.AddHours(-3) : null, IdEmpleadoRegistroTrabajo = registrada ? c.Empleado : null,
            FechaRealizada = estado == 8 ? Ahora.AddHours(-2) : null, FechaCancelacion = estado == 9 ? Ahora.AddHours(-2) : null };
        db.TareasOperativas.Add(t); await db.SaveChangesAsync(Ct); return t;
    }
    private async Task<RevisarDireccionSolicitud> Decisiones(HttpClient admin, int id, params int[] mover)
    {
        var r = (await admin.GetFromJsonAsync<RevisionDireccionRespuesta>(Revision(id), Ct))!;
        return new() { ClaveRevision = Guid.NewGuid(), IdEstadoAnterior = r.IdEstado, IdUbicacionEsperada = r.IdOrigen,
            Decisiones = r.Tareas.Select(t => new DecisionUbicacionSolicitud(t.IdTarea, t.IdUbicacion, mover.Contains(t.IdTarea))).ToList() };
    }
    private async Task<SolicitudPropiaRespuesta> Crear(HttpClient h, Caso c, CambioDireccionSolicitud p)
    {
        using var r = await h.PostAsJsonAsync(Direccion(c.Servicio), p, Ct); Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<SolicitudPropiaRespuesta>(Ct))!;
    }

    [Fact]
    public async Task Informado_valida_sin_tarea_y_conserva_historial_fecha_real_y_decisiones()
    {
        var c = await Preparar(); var mover = await Tarea(c); var conservar = await Tarea(c);
        var realizada = await Tarea(c, 8, true); var cancelada = await Tarea(c, 9);
        using var h = datos.Http(c.Token); using var admin = await Admin(); var pedido = Cambio(c);
        var s = await Crear(h, c, pedido);
        using (var db = app.NuevoContexto()) Assert.True((await db.UbicacionesServicio.SingleAsync(u => u.IdUbicacion == c.Origen, Ct)).EsActual);
        var revision = await Decisiones(admin, s.IdSolicitud, mover.IdTarea);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync(Revision(s.IdSolicitud), revision, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync(Revision(s.IdSolicitud), revision, Ct)).StatusCode);
        revision.Explicacion = "CAMBIADA";
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Revision(s.IdSolicitud), revision, Ct)).StatusCode);
        using var final = app.NuevoContexto(); var origen = await final.UbicacionesServicio.SingleAsync(u => u.IdUbicacion == c.Origen, Ct);
        var destino = await final.UbicacionesServicio.SingleAsync(u => u.IdServicio == c.Servicio && u.EsActual, Ct);
        Assert.False(origen.EsActual); Assert.Equal(pedido.FechaEfectiva, origen.FechaHasta); Assert.Equal(pedido.FechaEfectiva, destino.FechaDesde);
        Assert.Equal(Ahora, destino.FechaValidacion); Assert.Equal(Ahora, destino.FechaRegistro);
        Assert.Equal(destino.IdUbicacion, (await final.TareasOperativas.SingleAsync(t => t.IdTarea == mover.IdTarea, Ct)).IdUbicacion);
        foreach (var t in new[] { conservar, realizada, cancelada }) Assert.Equal(c.Origen, (await final.TareasOperativas.SingleAsync(a => a.IdTarea == t.IdTarea, Ct)).IdUbicacion);
        Assert.False(await final.TareasOperativas.AnyAsync(t => t.IdSolicitud == s.IdSolicitud, Ct));
        Assert.Equal("Resuelta", (await h.GetFromJsonAsync<SolicitudPropiaRespuesta>($"/api/solicitudes-cliente/{s.IdSolicitud}", Ct))!.Estado);
        Assert.Equal(HttpStatusCode.OK, (await h.PostAsJsonAsync(Direccion(c.Servicio), pedido, Ct)).StatusCode);
        Assert.Equal(2, await final.UbicacionesServicio.CountAsync(u => u.IdServicio == c.Servicio, Ct));
    }

    [Fact]
    public async Task Solicitado_requiere_revision_actual_y_bloquea_trabajos_en_curso_o_registrados()
    {
        var c = await Preparar(); var pendiente = await Tarea(c); using var h = datos.Http(c.Token); using var admin = await Admin();
        var s = await Crear(h, c, Cambio(c, "Solicitado"));
        var asignar = new AsignarSolicitud { ClaveProgramacion = Guid.NewGuid(), IdEmpleado = c.Empleado, IdEstadoAnterior = 1,
            IdServicioEsperado = c.Servicio, IdUbicacionEsperada = c.Origen, Fecha = new DateOnly(2030, 4, 4) };
        using var r = await admin.PostAsJsonAsync($"/api/solicitudes/{s.IdSolicitud}/asignar", asignar, Ct); Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var tarea = Assert.Single((await r.Content.ReadFromJsonAsync<SolicitudDetalleRespuesta>(Ct))!.Trabajos).IdTarea;
        using var empleado = datos.Http(c.TokenEmpleado);
        Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{tarea}/registrar-trabajo", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await empleado.PostAsync($"/api/tareas/{tarea}/finalizar", null, Ct)).StatusCode);
        var decisiones = await Decisiones(admin, s.IdSolicitud, pendiente.IdTarea);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync(Revision(s.IdSolicitud), decisiones, Ct)).StatusCode);
        var nueva = await Tarea(c);
        Assert.Equal(HttpStatusCode.Conflict, (await empleado.PostAsync($"/api/tareas/{tarea}/finalizar", null, Ct)).StatusCode);
        using (var db = app.NuevoContexto()) { var t = await db.TareasOperativas.SingleAsync(t => t.IdTarea == nueva.IdTarea, Ct); t.IdEstadoOperacion = 7; await db.SaveChangesAsync(Ct); }
        var consulta = (await admin.GetFromJsonAsync<RevisionDireccionRespuesta>(Revision(s.IdSolicitud), Ct))!;
        Assert.True(Assert.Single(consulta.Tareas, t => t.IdTarea == nueva.IdTarea).ImpideContinuar);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Revision(s.IdSolicitud), await Decisiones(admin, s.IdSolicitud), Ct)).StatusCode);
        using (var db = app.NuevoContexto()) { var t = await db.TareasOperativas.SingleAsync(t => t.IdTarea == nueva.IdTarea, Ct); t.IdEstadoOperacion = 6; t.FechaTrabajoRegistrado = Ahora; t.IdEmpleadoRegistroTrabajo = c.Empleado; await db.SaveChangesAsync(Ct); }
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(Revision(s.IdSolicitud), await Decisiones(admin, s.IdSolicitud), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{nueva.IdTarea}/finalizar", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync(Revision(s.IdSolicitud), await Decisiones(admin, s.IdSolicitud, pendiente.IdTarea), Ct)).StatusCode);
        using (var db = app.NuevoContexto()) Assert.True((await db.UbicacionesServicio.SingleAsync(u => u.IdUbicacion == c.Origen, Ct)).EsActual);
        foreach (var _ in Enumerable.Range(0, 2)) Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{tarea}/finalizar", null, Ct)).StatusCode);
        using var final = app.NuevoContexto();
        var actual = await final.UbicacionesServicio.SingleAsync(u => u.IdServicio == c.Servicio && u.EsActual, Ct);
        Assert.Equal(actual.IdUbicacion, (await final.TareasOperativas.SingleAsync(t => t.IdTarea == pendiente.IdTarea, Ct)).IdUbicacion);
        Assert.Equal(c.Origen, (await final.TareasOperativas.SingleAsync(t => t.IdTarea == nueva.IdTarea, Ct)).IdUbicacion);
        Assert.Equal(c.Origen, (await final.TareasOperativas.SingleAsync(t => t.IdTarea == tarea, Ct)).IdUbicacion);
        Assert.Equal("Resuelta", (await h.GetFromJsonAsync<SolicitudPropiaRespuesta>($"/api/solicitudes-cliente/{s.IdSolicitud}", Ct))!.Estado);
    }

    [Fact]
    public async Task Baja_cliente_conecta_con_retiro_sin_liberar_al_enviar_y_no_se_duplica()
    {
        var c = await Preparar(); using var h = datos.Http(c.Token); using var admin = await Admin(); var p = new BajaClienteSolicitud { ClaveEnvio = Guid.NewGuid() };
        using var r = await h.PostAsJsonAsync(Baja(c.Servicio), p, Ct); Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var s = (await r.Content.ReadFromJsonAsync<SolicitudPropiaRespuesta>(Ct))!;
        Assert.Equal(HttpStatusCode.Conflict, (await h.PostAsJsonAsync(Baja(c.Servicio), new BajaClienteSolicitud { ClaveEnvio = Guid.NewGuid() }, Ct)).StatusCode);
        var retiro = new AsignarSolicitud { ClaveProgramacion = Guid.NewGuid(), IdEmpleado = c.Empleado, IdEstadoAnterior = 1,
            IdServicioEsperado = c.Servicio, IdUbicacionEsperada = c.Origen, Fecha = new DateOnly(2030, 4, 4) };
        using var a = await admin.PostAsJsonAsync($"/api/solicitudes/{s.IdSolicitud}/asignar", retiro, Ct); Assert.Equal(HttpStatusCode.OK, a.StatusCode);
        using (var db = app.NuevoContexto()) Assert.Equal("Entregado", (await db.Servicios.SingleAsync(v => v.IdServicio == c.Servicio, Ct)).EstadoServicio);
        var tarea = Assert.Single((await a.Content.ReadFromJsonAsync<SolicitudDetalleRespuesta>(Ct))!.Trabajos).IdTarea;
        using var e = datos.Http(c.TokenEmpleado);
        foreach (var accion in new[] { "registrar-trabajo", "finalizar" }) Assert.Equal(HttpStatusCode.OK, (await e.PostAsync($"/api/tareas/{tarea}/{accion}", null, Ct)).StatusCode);
        Assert.Equal("Resuelta", (await h.GetFromJsonAsync<SolicitudPropiaRespuesta>($"/api/solicitudes-cliente/{s.IdSolicitud}", Ct))!.Estado);
        Assert.Empty((await h.GetFromJsonAsync<List<ServicioClienteRespuesta>>("/api/servicios-cliente/mios", Ct))!);
        Assert.Equal(HttpStatusCode.OK, (await h.PostAsJsonAsync(Baja(c.Servicio), p, Ct)).StatusCode);
    }

    [Fact]
    public async Task Concurrencia_direccion_baja_y_retiro_administrativo_identifica_incompatibles()
    {
        var c = await Preparar(); using var h = datos.Http(c.Token); using var admin = await Admin();
        var respuestas = await Task.WhenAll(h.PostAsJsonAsync(Direccion(c.Servicio), Cambio(c), Ct),
            h.PostAsJsonAsync(Baja(c.Servicio), new BajaClienteSolicitud { ClaveEnvio = Guid.NewGuid() }, Ct));
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Created); var conflicto = Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Contains("Solicitud", await conflicto.Content.ReadAsStringAsync(Ct));
        foreach (var r in respuestas) r.Dispose();
        var c2 = await Preparar(); using var h2 = datos.Http(c2.Token); await Crear(h2, c2, Cambio(c2, "Solicitado"));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/servicios/{c2.Servicio}/retiro", new ProgramarRetiroSolicitud {
            ClaveProgramacion = Guid.NewGuid(), IdEmpleado = c2.Empleado, IdUbicacionEsperada = c2.Origen, Fecha = new DateOnly(2030, 4, 4) }, Ct)).StatusCode);
        var c3 = await Preparar(); using var h3 = datos.Http(c3.Token);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/servicios/{c3.Servicio}/retiro", new ProgramarRetiroSolicitud {
            ClaveProgramacion = Guid.NewGuid(), IdEmpleado = c3.Empleado, IdUbicacionEsperada = c3.Origen, Fecha = new DateOnly(2030, 4, 4) }, Ct)).StatusCode);
        using var denegada = await h3.PostAsJsonAsync(Direccion(c3.Servicio), Cambio(c3), Ct);
        Assert.Equal(HttpStatusCode.Conflict, denegada.StatusCode); Assert.Contains("Tarea", await denegada.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Regresion_limpieza_no_es_retiro_y_retiro_correcto_actualiza_solicitud_servicios_y_contador()
    {
        var c = await Preparar(); var limpieza = await Tarea(c); var mantenimiento = await Tarea(c);
        using (var db = app.NuevoContexto())
        {
            var m = await db.TareasOperativas.SingleAsync(t => t.IdTarea == mantenimiento.IdTarea, Ct);
            m.IdTipoOperacion = 11; m.BloqueaAsignacion = true; await db.SaveChangesAsync(Ct);
        }
        using var cliente = datos.Http(c.Token); using var empleado = datos.Http(c.TokenEmpleado); using var admin = await Admin();
        foreach (var accion in new[] { "registrar-trabajo", "finalizar" })
            Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{limpieza.IdTarea}/{accion}", null, Ct)).StatusCode);
        using (var db = app.NuevoContexto())
        {
            Assert.Equal("Entregado", (await db.Servicios.SingleAsync(s => s.IdServicio == c.Servicio, Ct)).EstadoServicio);
            Assert.False(await db.SolicitudesCliente.AnyAsync(s => s.IdServicioReferencia == c.Servicio && s.IdTipoOperacion == 6, Ct));
        }
        using var solicitud = await cliente.PostAsJsonAsync(Baja(c.Servicio), new BajaClienteSolicitud { ClaveEnvio = Guid.NewGuid() }, Ct);
        Assert.Equal(HttpStatusCode.Created, solicitud.StatusCode);
        var idSolicitud = (await solicitud.Content.ReadFromJsonAsync<SolicitudPropiaRespuesta>(Ct))!.IdSolicitud;
        using var asignada = await admin.PostAsJsonAsync($"/api/solicitudes/{idSolicitud}/asignar", new AsignarSolicitud {
            ClaveProgramacion = Guid.NewGuid(), IdEmpleado = c.Empleado, IdEstadoAnterior = 1,
            IdServicioEsperado = c.Servicio, IdUbicacionEsperada = c.Origen, Fecha = new DateOnly(2030, 4, 4) }, Ct);
        Assert.Equal(HttpStatusCode.OK, asignada.StatusCode);
        var retiro = Assert.Single((await asignada.Content.ReadFromJsonAsync<SolicitudDetalleRespuesta>(Ct))!.Trabajos).IdTarea;
        var bloqueante = await Tarea(c);
        var dto = (await empleado.GetFromJsonAsync<BanosQuimicos.Api.Contratos.Tareas.TareaAsignadaRespuesta>($"/api/tareas/{retiro}", Ct))!;
        Assert.Equal(c.Servicio, dto.IdServicio); Assert.Equal(idSolicitud, dto.IdSolicitud); Assert.Equal("Retiro", dto.Tipo);
        Assert.Null(dto.FechaTrabajoRegistrado); Assert.Null(dto.FechaRealizada);
        Assert.Equal(bloqueante.IdTarea, Assert.Single(dto.ImpedimentosRetiro!).IdTarea);
        Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{retiro}/registrar-trabajo", null, Ct)).StatusCode);
        using var denegado = await empleado.PostAsync($"/api/tareas/{retiro}/finalizar", null, Ct);
        Assert.Equal(HttpStatusCode.Conflict, denegado.StatusCode); Assert.Contains($"tarea {bloqueante.IdTarea}", await denegado.Content.ReadAsStringAsync(Ct));
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/servicios/{c.Servicio}/resolver-pendientes", new ResolverPendientesSolicitud {
            IdCancelar = [bloqueante.IdTarea], IdConservar = [mantenimiento.IdTarea] }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{retiro}/finalizar", null, Ct)).StatusCode);
        Assert.Equal("Resuelta", (await cliente.GetFromJsonAsync<SolicitudPropiaRespuesta>($"/api/solicitudes-cliente/{idSolicitud}", Ct))!.Estado);
        Assert.Equal("Resuelta", Assert.Single((await cliente.GetFromJsonAsync<List<SolicitudPropiaRespuesta>>("/api/solicitudes-cliente/mias", Ct))!).Estado);
        Assert.Empty((await cliente.GetFromJsonAsync<List<ServicioClienteRespuesta>>("/api/servicios-cliente/mios", Ct))!);
        Assert.Equal(0, (await cliente.GetFromJsonAsync<BanosQuimicos.App.Autenticacion.ResumenServiciosCliente>("/api/servicios-cliente/mios/resumen", Ct))!.Cantidad);
        var final = (await admin.GetFromJsonAsync<ServicioGestionRespuesta>($"/api/servicios/{c.Servicio}", Ct))!;
        Assert.Equal("Finalizado", final.Estado); Assert.False(final.DisponibleAhora);
        using var contexto = app.NuevoContexto();
        Assert.Equal(6, (await contexto.TareasOperativas.SingleAsync(t => t.IdTarea == mantenimiento.IdTarea, Ct)).IdEstadoOperacion);
    }

    [Fact]
    public async Task Valida_fecha_direccion_propiedad_y_claves_sin_revelar_ajenos()
    {
        var c = await Preparar(); using var h = datos.Http(c.Token); var otro = await datos.Usuario("Cliente"); using var ajeno = datos.Http(otro.Token);
        foreach (var cambiar in new Action<CambioDireccionSolicitud>[] { d => d.FechaEfectiva = Ahora.AddMinutes(1), d => d.FechaEfectiva = null,
            d => d.FechaEfectiva = Ahora.AddDays(-4), d => d.NuevaDireccion = "  ", d => d.NuevaDireccion = new string('X', 301),
            d => d.Modalidad = "Otra", d => d.ClaveEnvio = Guid.Empty })
        { var p = Cambio(c); cambiar(p); Assert.Equal(HttpStatusCode.BadRequest, (await h.PostAsJsonAsync(Direccion(c.Servicio), p, Ct)).StatusCode); }
        Assert.Equal(HttpStatusCode.NotFound, (await ajeno.PostAsJsonAsync(Direccion(c.Servicio), Cambio(c), Ct)).StatusCode);
        var pedido = Cambio(c); var s = await Crear(h, c, pedido);
        Assert.Equal(HttpStatusCode.Conflict, (await ajeno.PostAsJsonAsync(Direccion(c.Servicio), pedido, Ct)).StatusCode);
        pedido.NuevaDireccion += " CAMBIADA"; Assert.Equal(HttpStatusCode.Conflict, (await h.PostAsJsonAsync(Direccion(c.Servicio), pedido, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await h.PostAsJsonAsync(Revision(s.IdSolicitud), new RevisarDireccionSolicitud(), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ajeno.GetAsync($"/api/solicitudes-cliente/{s.IdSolicitud}", Ct)).StatusCode);
    }

    [Theory]
    [InlineData("SinSesion", 401)] [InlineData("Administrador", 403)] [InlineData("Empleado", 403)]
    [InlineData("Pendiente", 403)] [InlineData("Suspendida", 401)] [InlineData("BajaLogica", 401)]
    public async Task Acciones_nuevas_exigen_cliente_activo(string tipo, int codigo)
    {
        string? token = null;
        if (tipo == "Administrador") token = await datos.Token(app.CorreoAdmin, app.ClaveAdmin);
        else if (tipo != "SinSesion")
        {
            var c = await datos.Usuario(tipo == "Empleado" ? "Empleado" : "Cliente", tipo != "Pendiente"); token = c.Token;
            if (tipo is "Suspendida" or "BajaLogica") { using var db = app.NuevoContexto(); var u = await db.Usuarios.SingleAsync(u => u.IdUsuario == c.Id, Ct); u.EstadoCuenta = tipo; await db.SaveChangesAsync(Ct); }
        }
        using var h = datos.Http(token);
        Assert.Equal((HttpStatusCode)codigo, (await h.PostAsJsonAsync(Baja(1), new BajaClienteSolicitud(), Ct)).StatusCode);
        Assert.Equal((HttpStatusCode)codigo, (await h.PostAsJsonAsync(Direccion(1), new CambioDireccionSolicitud(), Ct)).StatusCode);
        if (tipo != "Administrador") Assert.Equal((HttpStatusCode)codigo, (await h.GetAsync(Revision(1), Ct)).StatusCode);
    }
}
