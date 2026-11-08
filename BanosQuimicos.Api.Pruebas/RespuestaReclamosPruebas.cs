using System.Net;
using System.Net.Http.Json;
using BanosQuimicos.Api.Contratos.Solicitudes;
using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AppAuth = global::BanosQuimicos.App.Autenticacion;

namespace BanosQuimicos.Api.Pruebas;

public class RespuestaReclamosPruebas(AplicacionTareasPruebas app) : IClassFixture<AplicacionTareasPruebas>
{
    private readonly TareasEmpleadoPruebas datos = new(app);
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private async Task<HttpClient> Admin() => datos.Http(await datos.Token(app.CorreoAdmin, app.ClaveAdmin));
    private record Caso(int Solicitud, int Servicio, int Bano, int Ubicacion, string ClienteToken, int Empleado, string EmpleadoToken);
    private async Task<Caso> Preparar()
    {
        var c = await datos.Usuario("Cliente"); var e = await datos.Usuario(); using var db = app.NuevoContexto();
        var s = new Servicio { IdCliente = await db.Clientes.Where(x => x.IdUsuario == c.Id).Select(x => x.IdCliente).SingleAsync(Ct),
            Bano = new BanoQuimico { CodigoInterno = "RR-" + Guid.NewGuid().ToString("N")[..12], IdTipoBano = 1, IdEstadoBano = 1,
                IdResponsableLimpieza = e.Id, FechaAlta = AplicacionTareasPruebas.Ahora.AddDays(-4) },
            TipoUso = "Obra", EstadoServicio = "Entregado", FechaAsignacion = AplicacionTareasPruebas.Ahora.AddDays(-3), FechaEntregaReal = AplicacionTareasPruebas.Ahora.AddDays(-2) };
        var u = new UbicacionServicio { Servicio = s, EsActual = true, Direccion = "PRUEBA RESPUESTA RECLAMO 123",
            FechaRegistro = AplicacionTareasPruebas.Ahora.AddDays(-2), FechaDesde = AplicacionTareasPruebas.Ahora.AddDays(-2) };
        db.Add(u); await db.SaveChangesAsync(Ct);
        using var h = datos.Http(c.Token);
        var r = await h.PostAsJsonAsync($"/api/solicitudes-cliente/servicios/{s.IdServicio}/reclamo",
            new { claveEnvio = Guid.NewGuid(), descripcion = "PRUEBA RECLAMO: aclaración del servicio" }, Ct);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var solicitud = (await r.Content.ReadFromJsonAsync<SolicitudPropiaRespuesta>(Ct))!;
        return new(solicitud.IdSolicitud, s.IdServicio, s.IdBano, u.IdUbicacion, c.Token, e.Id, e.Token);
    }
    private Task<HttpResponseMessage> Responder(HttpClient a, Caso c, string respuesta = "PRUEBA RESPUESTA: aclaración sin trabajo físico", int estado = 1) =>
        a.PostAsJsonAsync($"/api/solicitudes/{c.Solicitud}/responder-resolver", new { respuesta, idEstadoAnterior = estado }, Ct);
    private AsignarSolicitud Asignacion(Caso c) => new() { ClaveProgramacion = Guid.NewGuid(), IdEmpleado = c.Empleado,
        IdEstadoAnterior = 1, IdServicioEsperado = c.Servicio, IdUbicacionEsperada = c.Ubicacion,
        IdTipoTareaReclamo = 11, BloqueaAsignacion = true, Fecha = new(2030, 4, 4) };

    [Fact]
    public async Task Cliente_crea_admin_responde_y_solo_el_propietario_consulta_respuesta_sin_efectos_operativos()
    {
        var c = await Preparar(); using var h = datos.Http();
        var sesion = new AppAuth.SesionUsuario(new AppAuth.ClienteAutenticacion(h), new AutenticacionAppPruebas.AlmacenPrueba());
        await sesion.InicializarAsync(); await sesion.IngresarAsync(new() { Email = app.CorreoAdmin, Contrasena = app.ClaveAdmin });
        var resuelta = await sesion.ResponderReclamoAsync(c.Solicitud, new("  PRUEBA RESPUESTA: aclaración sin trabajo físico  ", 1));
        Assert.Equal("Resuelta", resuelta.Estado); Assert.Equal(AplicacionTareasPruebas.Ahora, resuelta.FechaResolucion);
        using var cliente = datos.Http(c.ClienteToken);
        var propia = (await cliente.GetFromJsonAsync<AppAuth.SolicitudPropia>($"/api/solicitudes-cliente/{c.Solicitud}", Ct))!;
        Assert.Equal(resuelta.ObservacionesAdministrador, propia.RespuestaAdministrativa); Assert.Equal("Resuelta", propia.Estado);
        Assert.Equal(resuelta.FechaResolucion, propia.FechaResolucion);
        Assert.Equal(propia.RespuestaAdministrativa, Assert.Single((await cliente.GetFromJsonAsync<List<AppAuth.SolicitudPropia>>("/api/solicitudes-cliente/mias", Ct))!).RespuestaAdministrativa);
        var otro = await datos.Usuario("Cliente"); using var ajeno = datos.Http(otro.Token);
        Assert.Equal(HttpStatusCode.NotFound, (await ajeno.GetAsync($"/api/solicitudes-cliente/{c.Solicitud}", Ct)).StatusCode);
        Assert.Empty((await ajeno.GetFromJsonAsync<List<SolicitudPropiaRespuesta>>($"/api/solicitudes-cliente/mias?idCliente={c.Solicitud}", Ct))!);
        using var db = app.NuevoContexto(); var s = await db.Servicios.SingleAsync(x => x.IdServicio == c.Servicio, Ct);
        Assert.Equal("Entregado", s.EstadoServicio); Assert.Null(s.FechaFinalizacion); Assert.Null(s.FechaRetiroReal);
        Assert.False(await db.TareasOperativas.AnyAsync(t => t.IdSolicitud == c.Solicitud, Ct));
        var b = await db.BanosQuimicos.SingleAsync(x => x.IdBano == c.Bano, Ct); Assert.Equal(1, b.IdEstadoBano); Assert.Equal(c.Empleado, b.IdResponsableLimpieza);
        Assert.Single(await db.UbicacionesServicio.Where(x => x.IdServicio == c.Servicio && x.EsActual && x.IdUbicacion == c.Ubicacion).ToListAsync(Ct));
        Assert.Equal("PRUEBA RECLAMO: aclaración del servicio", (await db.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == c.Solicitud, Ct)).Descripcion);
    }

    [Fact]
    public async Task Reintentos_concurrentes_no_reemplazan_respuesta_fecha_ni_otra_resolucion()
    {
        var c = await Preparar(); using var a = await Admin();
        var rs = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => Responder(a, c)));
        foreach (var r in rs) { Assert.Equal(HttpStatusCode.OK, r.StatusCode); r.Dispose(); }
        Assert.Equal(HttpStatusCode.Conflict, (await Responder(a, c, "Otra respuesta")).StatusCode);
        var distinto = await Preparar(); var cambios = await Task.WhenAll(Responder(a, distinto, "Respuesta A"), Responder(a, distinto, "Respuesta B"));
        Assert.Single(cambios, r => r.StatusCode == HttpStatusCode.OK); Assert.Single(cambios, r => r.StatusCode == HttpStatusCode.Conflict);
        foreach (var r in cambios) r.Dispose();
        using var db = app.NuevoContexto(); var final = await db.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == c.Solicitud, Ct);
        Assert.Equal(AplicacionTareasPruebas.Ahora, final.FechaResolucion); Assert.Equal("PRUEBA RESPUESTA: aclaración sin trabajo físico", final.ObservacionesAdministrador);
        Assert.False(await db.TareasOperativas.AnyAsync(t => t.IdSolicitud == c.Solicitud, Ct));
    }

    [Theory]
    [InlineData(6, false)] [InlineData(7, false)] [InlineData(6, true)] [InlineData(9, true)]
    public async Task Trabajo_activo_o_registrado_sin_finalizar_impide_respuesta_y_conserva_datos(int estado, bool registrado)
    {
        var c = await Preparar(); using var a = await Admin();
        Assert.Equal(HttpStatusCode.OK, (await a.PostAsJsonAsync($"/api/solicitudes/{c.Solicitud}/asignar", Asignacion(c), Ct)).StatusCode);
        using var db = app.NuevoContexto(); var t = await db.TareasOperativas.SingleAsync(x => x.IdSolicitud == c.Solicitud, Ct);
        t.IdEstadoOperacion = estado;
        if (registrado) { t.IdEmpleadoRegistroTrabajo = c.Empleado; t.FechaTrabajoRegistrado = AplicacionTareasPruebas.Ahora; }
        if (estado == 9) t.FechaCancelacion = AplicacionTareasPruebas.Ahora;
        await db.SaveChangesAsync(Ct);
        var r = await Responder(a, c, estado: 2); Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Contains($"Tarea {t.IdTarea}", await r.Content.ReadAsStringAsync(Ct));
        var final = await db.SolicitudesCliente.AsNoTracking().SingleAsync(x => x.IdSolicitud == c.Solicitud, Ct);
        Assert.Equal(2, final.IdEstadoOperacion); Assert.Null(final.FechaResolucion); Assert.Null(final.ObservacionesAdministrador);
        Assert.Equal(estado, (await db.TareasOperativas.AsNoTracking().SingleAsync(x => x.IdTarea == t.IdTarea, Ct)).IdEstadoOperacion);
    }

    [Fact]
    public async Task Responder_y_asignar_simultaneamente_no_omiten_trabajos()
    {
        var c = await Preparar(); using var a = await Admin();
        var rs = await Task.WhenAll(Responder(a, c), a.PostAsJsonAsync($"/api/solicitudes/{c.Solicitud}/asignar", Asignacion(c), Ct));
        Assert.Single(rs, r => r.StatusCode == HttpStatusCode.OK); Assert.Single(rs, r => r.StatusCode == HttpStatusCode.Conflict);
        foreach (var r in rs) r.Dispose();
        using var db = app.NuevoContexto(); var final = await db.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == c.Solicitud, Ct);
        Assert.Equal(final.IdEstadoOperacion == 2, await db.TareasOperativas.AnyAsync(x => x.IdSolicitud == c.Solicitud, Ct));
        Assert.Equal(final.IdEstadoOperacion == 4, final.ObservacionesAdministrador is not null);
    }

    [Theory]
    [InlineData("Anonimo", 401)] [InlineData("Cliente", 403)] [InlineData("Empleado", 403)] [InlineData("Pendiente", 403)] [InlineData("Suspendida", 401)]
    public async Task Solo_administrador_activo_responde(string tipo, int codigo)
    {
        var c = await Preparar(); string? token = null;
        if (tipo != "Anonimo")
        {
            var u = await datos.Usuario(tipo == "Empleado" ? "Empleado" : "Cliente", tipo != "Pendiente"); token = u.Token;
            if (tipo == "Suspendida") { using var db = app.NuevoContexto(); var usuario = await db.Usuarios.SingleAsync(x => x.IdUsuario == u.Id, Ct);
                usuario.IdRol = await db.Roles.Where(r => r.NombreRol == "Administrador").Select(r => r.IdRol).SingleAsync(Ct); usuario.EstadoCuenta = "Suspendida"; await db.SaveChangesAsync(Ct); }
        }
        using var actor = datos.Http(token); Assert.Equal(codigo, (int)(await Responder(actor, c)).StatusCode);
    }

    [Fact]
    public async Task Rechaza_datos_invalidos_otros_tipos_estados_cerrados_y_sobrescritura_de_historial()
    {
        var c = await Preparar(); using var a = await Admin();
        foreach (var texto in new[] { "   ", new string('x', 2001) }) Assert.Equal(HttpStatusCode.BadRequest, (await Responder(a, c, texto)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Responder(a, c, estado: 2)).StatusCode);
        using var db = app.NuevoContexto(); var s = await db.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == c.Solicitud, Ct);
        foreach (var tipo in new[] { 1, 3, 4, 6 }) { s.IdTipoOperacion = tipo; await db.SaveChangesAsync(Ct); Assert.Equal(HttpStatusCode.Conflict, (await Responder(a, c)).StatusCode); }
        s.IdTipoOperacion = 5;
        foreach (var estado in new[] { 3, 4, 5 }) { s.IdEstadoOperacion = estado; await db.SaveChangesAsync(Ct); Assert.Equal(HttpStatusCode.Conflict, (await Responder(a, c)).StatusCode); }
        s.IdEstadoOperacion = 1; s.ObservacionesAdministrador = "Explicación previa conservada"; await db.SaveChangesAsync(Ct);
        Assert.Equal(HttpStatusCode.Conflict, (await Responder(a, c)).StatusCode);
        Assert.Equal("Explicación previa conservada", (await db.SolicitudesCliente.AsNoTracking().SingleAsync(x => x.IdSolicitud == c.Solicitud, Ct)).ObservacionesAdministrador);
    }
}
