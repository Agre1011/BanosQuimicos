using System.Net;
using System.Net.Http.Json;
using BanosQuimicos.Api.Contratos.Solicitudes;
using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ClienteApp = global::BanosQuimicos.App.Autenticacion;

namespace BanosQuimicos.Api.Pruebas;

public class LimpiezaReclamosClientePruebas(AplicacionTareasPruebas app) : IClassFixture<AplicacionTareasPruebas>
{
    private readonly TareasEmpleadoPruebas datos = new(app);
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private static string Ruta(int id, bool reclamo) => $"/api/solicitudes-cliente/servicios/{id}/{(reclamo ? "reclamo" : "limpieza-extra")}";
    private static SolicitudServicioSolicitud Envio(bool reclamo) => new() { ClaveEnvio = Guid.NewGuid(), Descripcion = reclamo ? "PRUEBA CLIENTE: falta papel después de la limpieza." : null };
    private record Caso(int Servicio, int Cliente, string Token, int Empleado, string TokenEmpleado, int Ubicacion, int Bano);
    private async Task<Caso> Preparar()
    {
        var c = await datos.Usuario("Cliente"); var e = await datos.Usuario(); using var db = app.NuevoContexto();
        var s = new Servicio { IdCliente = await db.Clientes.Where(x => x.IdUsuario == c.Id).Select(x => x.IdCliente).SingleAsync(Ct),
            Bano = new BanoQuimico { CodigoInterno = "LC-" + Guid.NewGuid().ToString("N")[..12], IdTipoBano = 1, IdEstadoBano = 1,
                IdResponsableLimpieza = e.Id, FechaAlta = AplicacionTareasPruebas.Ahora.AddDays(-4) },
            TipoUso = "Obra", EstadoServicio = "Entregado", FechaAsignacion = AplicacionTareasPruebas.Ahora.AddDays(-3), FechaEntregaReal = AplicacionTareasPruebas.Ahora.AddDays(-2) };
        var u = new UbicacionServicio { Servicio = s, EsActual = true, Direccion = "PRUEBA LIMPIEZA RECLAMO 123",
            FechaRegistro = AplicacionTareasPruebas.Ahora.AddDays(-2), FechaDesde = AplicacionTareasPruebas.Ahora.AddDays(-2) };
        db.UbicacionesServicio.Add(u); await db.SaveChangesAsync(Ct);
        return new(s.IdServicio, c.Id, c.Token, e.Id, e.Token, u.IdUbicacion, s.IdBano);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Envio_cliente_conecta_con_asignacion_y_cumplimiento_existentes(bool reclamo)
    {
        var c = await Preparar(); using var h = datos.Http(c.Token); var p = Envio(reclamo);
        using var r = await h.PostAsJsonAsync(Ruta(c.Servicio, reclamo), p, Ct);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var s = (await r.Content.ReadFromJsonAsync<SolicitudPropiaRespuesta>(Ct))!;
        Assert.Equal("Pendiente", s.Estado); Assert.Single(s.Banos);
        Assert.Equal(p.Descripcion, s.Descripcion);
        using var db = app.NuevoContexto();
        Assert.False(await db.TareasOperativas.AnyAsync(t => t.IdSolicitud == s.IdSolicitud, Ct));
        using var admin = datos.Http(await datos.Token(app.CorreoAdmin, app.ClaveAdmin));
        Assert.Contains((await admin.GetFromJsonAsync<List<SolicitudResumenRespuesta>>("/api/solicitudes", Ct))!, x => x.IdSolicitud == s.IdSolicitud);
        var asignacion = new AsignarSolicitud { ClaveProgramacion = Guid.NewGuid(), IdEmpleado = c.Empleado,
            IdEstadoAnterior = 1, IdServicioEsperado = c.Servicio, IdUbicacionEsperada = c.Ubicacion,
            Fecha = new DateOnly(2030, 4, 4), IdTipoTareaReclamo = reclamo ? 11 : null, BloqueaAsignacion = reclamo };
        using var ar = await admin.PostAsJsonAsync($"/api/solicitudes/{s.IdSolicitud}/asignar", asignacion, Ct);
        Assert.Equal(HttpStatusCode.OK, ar.StatusCode);
        var asignada = (await ar.Content.ReadFromJsonAsync<SolicitudDetalleRespuesta>(Ct))!;
        Assert.Equal("Aprobada", asignada.Estado);
        using var empleado = datos.Http(c.TokenEmpleado); var tarea = Assert.Single(asignada.Trabajos).IdTarea;
        foreach (var accion in new[] { "registrar-trabajo", "finalizar" })
            Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{tarea}/{accion}", null, Ct)).StatusCode);
        Assert.Equal("Resuelta", (await h.GetFromJsonAsync<SolicitudPropiaRespuesta>($"/api/solicitudes-cliente/{s.IdSolicitud}", Ct))!.Estado);
        Assert.Equal(c.Empleado, (await db.BanosQuimicos.SingleAsync(b => b.IdBano == c.Bano, Ct)).IdResponsableLimpieza);
    }

    [Fact]
    public async Task Claves_reintentos_y_propiedad_no_duplican_ni_revelan_solicitudes_ajenas()
    {
        var c = await Preparar(); var otro = await datos.Usuario("Cliente"); using var h = datos.Http(c.Token); using var ajeno = datos.Http(otro.Token);
        foreach (var reclamo in new[] { false, true })
        {
            var p = Envio(reclamo); var ruta = Ruta(c.Servicio, reclamo);
            var respuestas = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => h.PostAsJsonAsync(ruta, p, Ct)));
            Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.Created);
            Assert.Equal(2, respuestas.Count(r => r.StatusCode == HttpStatusCode.OK));
            var ids = new List<int>(); foreach (var r in respuestas) { ids.Add((await r.Content.ReadFromJsonAsync<SolicitudPropiaRespuesta>(Ct))!.IdSolicitud); r.Dispose(); }
            Assert.Single(ids.Distinct());
            Assert.Equal(HttpStatusCode.Conflict, (await ajeno.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ajeno.PostAsJsonAsync(ruta, Envio(reclamo), Ct)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ajeno.GetAsync($"/api/solicitudes-cliente/{ids[0]}", Ct)).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await h.PostAsJsonAsync(ruta, Envio(reclamo), Ct)).StatusCode); // Pedido nuevo intencional.
            if (reclamo) { p.Descripcion += " ALTERADO"; Assert.Equal(HttpStatusCode.Conflict, (await h.PostAsJsonAsync(ruta, p, Ct)).StatusCode); }
            else { Assert.Equal(HttpStatusCode.Conflict, (await h.PostAsJsonAsync(Ruta(c.Servicio, true), new SolicitudServicioSolicitud { ClaveEnvio = p.ClaveEnvio, Descripcion = "OTRO TIPO" }, Ct)).StatusCode); }
        }
        using var db = app.NuevoContexto();
        Assert.Equal(4, await db.SolicitudesCliente.CountAsync(s => s.IdServicioReferencia == c.Servicio, Ct));
        Assert.Empty((await ajeno.GetFromJsonAsync<List<SolicitudPropiaRespuesta>>("/api/solicitudes-cliente/mias", Ct))!);
    }

    [Fact]
    public async Task Valida_descripcion_y_estados_sin_perder_reintento_de_un_guardado_anterior()
    {
        var c = await Preparar(); using var h = datos.Http(c.Token);
        foreach (var texto in new[] { "", "   ", new string('X', 2001) })
            Assert.Equal(HttpStatusCode.BadRequest, (await h.PostAsJsonAsync(Ruta(c.Servicio, true), new SolicitudServicioSolicitud { ClaveEnvio = Guid.NewGuid(), Descripcion = texto }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await h.PostAsJsonAsync(Ruta(c.Servicio, false), new SolicitudServicioSolicitud(), Ct)).StatusCode);
        var previo = Envio(false); Assert.Equal(HttpStatusCode.Created, (await h.PostAsJsonAsync(Ruta(c.Servicio, false), previo, Ct)).StatusCode);
        using var db = app.NuevoContexto(); var s = await db.Servicios.SingleAsync(s => s.IdServicio == c.Servicio, Ct);
        foreach (var estado in new[] { "Finalizado", "Cancelado", "Pendiente de entrega" })
        {
            s.EstadoServicio = estado; s.FechaEntregaReal = estado == "Finalizado" ? AplicacionTareasPruebas.Ahora : null;
            s.FechaRetiroReal = s.FechaFinalizacion = estado == "Finalizado" ? AplicacionTareasPruebas.Ahora : null;
            s.FechaCancelacion = estado == "Cancelado" ? AplicacionTareasPruebas.Ahora : null;
            await db.SaveChangesAsync(Ct);
            foreach (var reclamo in new[] { false, true })
                Assert.Equal(HttpStatusCode.Conflict, (await h.PostAsJsonAsync(Ruta(c.Servicio, reclamo), Envio(reclamo), Ct)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await h.PostAsJsonAsync(Ruta(c.Servicio, false), previo, Ct)).StatusCode);
    }

    [Theory]
    [InlineData("SinSesion", 401)] [InlineData("Administrador", 403)] [InlineData("Empleado", 403)]
    [InlineData("Pendiente", 403)] [InlineData("Suspendida", 401)] [InlineData("BajaLogica", 401)]
    public async Task Nuevas_rutas_exigen_cliente_activo(string tipo, int codigo)
    {
        string? token = null;
        if (tipo == "Administrador") token = await datos.Token(app.CorreoAdmin, app.ClaveAdmin);
        else if (tipo != "SinSesion")
        {
            var c = await datos.Usuario(tipo == "Empleado" ? "Empleado" : "Cliente", tipo != "Pendiente"); token = c.Token;
            if (tipo is "Suspendida" or "BajaLogica") { using var db = app.NuevoContexto(); var u = await db.Usuarios.SingleAsync(u => u.IdUsuario == c.Id, Ct); u.EstadoCuenta = tipo; await db.SaveChangesAsync(Ct); }
        }
        using var h = datos.Http(token);
        foreach (var reclamo in new[] { false, true })
            Assert.Equal((HttpStatusCode)codigo, (await h.PostAsJsonAsync(Ruta(1, reclamo), Envio(reclamo), Ct)).StatusCode);
    }

    [Fact]
    public async Task App_conserva_texto_y_clave_tras_validacion_y_renueva_solo_al_terminar_pedido()
    {
        var c = await datos.Usuario("Cliente"); using var h = datos.Http();
        var sesion = new ClienteApp.SesionUsuario(new ClienteApp.ClienteAutenticacion(h), new AutenticacionAppPruebas.AlmacenPrueba { Token = c.Token });
        await sesion.InicializarAsync(); var p = sesion.BorradorOperacion(1, true); p.Descripcion = "   ";
        await Assert.ThrowsAsync<ClienteApp.ErrorApi>(() => sesion.SolicitarSobreServicioAsync(1, true, p));
        p.Descripcion = "TEXTO CONSERVADO"; Assert.Same(p, sesion.BorradorOperacion(1, true));
        Assert.Equal("TEXTO CONSERVADO", sesion.BorradorOperacion(1, true).Descripcion);
        sesion.LimpiarBorradorOperacion(1, true); Assert.NotEqual(p.ClaveEnvio, sesion.BorradorOperacion(1, true).ClaveEnvio);
        await sesion.CerrarAsync(); await sesion.IngresarAsync(new ClienteApp.DatosLogin { Email = c.Email, Contrasena = c.Clave });
        Assert.NotSame(p, sesion.BorradorOperacion(1, true));
    }
}
