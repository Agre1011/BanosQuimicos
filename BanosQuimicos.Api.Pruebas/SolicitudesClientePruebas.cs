using System.Net;
using System.Net.Http.Json;
using BanosQuimicos.Api.Contratos.Solicitudes;
using BanosQuimicos.Api.Controllers;
using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ClienteApp = global::BanosQuimicos.App.Autenticacion;

namespace BanosQuimicos.Api.Pruebas;

public class SolicitudesClientePruebas(AplicacionTareasPruebas app) : IClassFixture<AplicacionTareasPruebas>
{
    private readonly TareasEmpleadoPruebas datos = new(app);
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private const string Crear = "/api/solicitudes-cliente/nuevo-servicio";
    private static NuevoServicioSolicitud Pedido() => new() { ClaveEnvio = Guid.NewGuid(), Direccion = "PRUEBA CLIENTE NUEVO 456",
        CantidadBanos = 2, IdTipoBano = 1, TipoUso = "Evento", FechaDeseada = new DateOnly(2030, 4, 4) };

    [Fact]
    public async Task Cliente_envia_y_consulta_entregas_parciales_hasta_resuelta_y_servicios_propios()
    {
        var c = await datos.Usuario("Cliente"); var e = await datos.Usuario(); using var http = datos.Http(c.Token);
        var pedido = Pedido(); using var r = await http.PostAsJsonAsync(Crear, pedido, Ct);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var s = (await r.Content.ReadFromJsonAsync<SolicitudPropiaRespuesta>(Ct))!;
        Assert.Equal("Pendiente", s.Estado); Assert.Equal("Evento", s.TipoUso); Assert.Equal("Obra", s.TipoBano); Assert.Empty(s.Banos);
        using var db = app.NuevoContexto();
        Assert.False(await db.Servicios.AnyAsync(v => v.IdSolicitudOrigen == s.IdSolicitud, Ct));
        Assert.False(await db.TareasOperativas.AnyAsync(t => t.IdSolicitud == s.IdSolicitud, Ct));
        var banos = Enumerable.Range(0, 2).Select(_ => new BanoQuimico { CodigoInterno = "PC-" + Guid.NewGuid().ToString("N")[..12],
            IdTipoBano = 1, IdEstadoBano = 1, FechaAlta = AplicacionTareasPruebas.Ahora }).ToList();
        db.BanosQuimicos.AddRange(banos); await db.SaveChangesAsync(Ct);
        using var admin = datos.Http(await datos.Token(app.CorreoAdmin, app.ClaveAdmin));
        Assert.Contains((await admin.GetFromJsonAsync<List<SolicitudResumenRespuesta>>("/api/solicitudes", Ct))!, a => a.IdSolicitud == s.IdSolicitud);
        var asignar = new AsignarBanosSolicitud { ClaveProgramacion = Guid.NewGuid(), IdBanos = banos.Select(b => b.IdBano).ToList(),
            IdEmpleado = e.Id, IdEstadoAnterior = 1, CantidadLimpiezasSemana = 1, Fecha = pedido.FechaDeseada };
        using var ar = await admin.PostAsJsonAsync($"/api/solicitudes/{s.IdSolicitud}/asignar-banos", asignar, Ct);
        Assert.Equal(HttpStatusCode.OK, ar.StatusCode);
        var asignada = (await ar.Content.ReadFromJsonAsync<SolicitudDetalleRespuesta>(Ct))!;
        using var empleado = datos.Http(e.Token);
        for (var i = 0; i < asignada.Trabajos.Count; i++)
        {
            var tarea = asignada.Trabajos[i].IdTarea;
            foreach (var accion in new[] { "registrar-trabajo", "finalizar" })
                Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{tarea}/{accion}", null, Ct)).StatusCode);
            var actual = (await http.GetFromJsonAsync<SolicitudPropiaRespuesta>($"/api/solicitudes-cliente/{s.IdSolicitud}", Ct))!;
            Assert.Equal(i == 0 ? "Aprobada" : "Resuelta", actual.Estado); Assert.Equal(2, actual.Banos.Count);
        }
        var servicios = (await http.GetFromJsonAsync<List<ServicioClienteRespuesta>>("/api/servicios-cliente/mios", Ct))!;
        Assert.Equal(2, servicios.Count); Assert.All(servicios, v => Assert.Equal("Entregado", v.EstadoServicio));
        using var repetida = await http.PostAsJsonAsync(Crear, pedido, Ct);
        Assert.Equal(HttpStatusCode.OK, repetida.StatusCode);
        Assert.Equal(s.IdSolicitud, (await repetida.Content.ReadFromJsonAsync<SolicitudPropiaRespuesta>(Ct))!.IdSolicitud);
    }

    [Fact]
    public async Task Reintentos_concurrentes_nueva_clave_y_aislamiento_de_clave_y_solicitudes()
    {
        var a = await datos.Usuario("Cliente"); var b = await datos.Usuario("Cliente");
        using var h = datos.Http(a.Token); using var otro = datos.Http(b.Token); var p = Pedido();
        var rs = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => h.PostAsJsonAsync(Crear, p, Ct)));
        Assert.Single(rs, r => r.StatusCode == HttpStatusCode.Created); Assert.Equal(2, rs.Count(r => r.StatusCode == HttpStatusCode.OK));
        var ids = new List<int>(); foreach (var r in rs) { ids.Add((await r.Content.ReadFromJsonAsync<SolicitudPropiaRespuesta>(Ct))!.IdSolicitud); r.Dispose(); }
        Assert.Single(ids.Distinct()); var id = ids[0];
        Assert.Equal(HttpStatusCode.Conflict, (await otro.PostAsJsonAsync(Crear, p, Ct)).StatusCode);
        Assert.Empty((await otro.GetFromJsonAsync<List<SolicitudPropiaRespuesta>>($"/api/solicitudes-cliente/mias?idCliente={a.Id}", Ct))!);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"/api/solicitudes-cliente/{id}", Ct)).StatusCode);
        p.Direccion += " CAMBIADA"; Assert.Equal(HttpStatusCode.Conflict, (await h.PostAsJsonAsync(Crear, p, Ct)).StatusCode);
        p = Pedido(); Assert.Equal(HttpStatusCode.Created, (await h.PostAsJsonAsync(Crear, p, Ct)).StatusCode);
        using var db = app.NuevoContexto();
        Assert.Equal(2, await db.SolicitudesCliente.CountAsync(s => s.Cliente.IdUsuario == a.Id, Ct));
        // No existe edición del cliente ni puede usar la escritura administrativa para cambiar una solicitud.
        Assert.Equal(HttpStatusCode.Forbidden, (await otro.PostAsJsonAsync($"/api/solicitudes/{id}/asignar-banos", new AsignarBanosSolicitud(), Ct)).StatusCode);
    }

    [Fact]
    public async Task Validaciones_rechazan_campos_invalidos_sin_guardar()
    {
        var c = await datos.Usuario("Cliente"); using var http = datos.Http(c.Token);
        foreach (var cambiar in new Action<NuevoServicioSolicitud>[] {
            p => p.ClaveEnvio = Guid.Empty, p => p.Direccion = "   ", p => p.Direccion = new string('X', 301),
            p => p.CantidadBanos = 0, p => p.IdTipoBano = int.MaxValue, p => p.TipoUso = "Otro", p => p.FechaDeseada = default })
        {
            var p = Pedido(); cambiar(p); Assert.Equal(HttpStatusCode.BadRequest, (await http.PostAsJsonAsync(Crear, p, Ct)).StatusCode);
        }
        using var db = app.NuevoContexto(); Assert.False(await db.SolicitudesCliente.AnyAsync(s => s.Cliente.IdUsuario == c.Id, Ct));
    }

    [Fact]
    public async Task Listado_incluye_otros_tipos_y_admite_varios_registros_sin_clave()
    {
        var c = await datos.Usuario("Cliente"); using var db = app.NuevoContexto();
        var cliente = await db.Clientes.Where(x => x.IdUsuario == c.Id).Select(x => x.IdCliente).SingleAsync(Ct);
        db.SolicitudesCliente.AddRange(new SolicitudCliente { IdCliente = cliente, IdTipoOperacion = 5, IdEstadoOperacion = 3, FechaSolicitud = AplicacionTareasPruebas.Ahora, Descripcion = "PRUEBA RECLAMO" },
            new SolicitudCliente { IdCliente = cliente, IdTipoOperacion = 4, IdEstadoOperacion = 5, FechaSolicitud = AplicacionTareasPruebas.Ahora, Descripcion = "PRUEBA CAMBIO" });
        await db.SaveChangesAsync(Ct); using var h = datos.Http(c.Token);
        var lista = (await h.GetFromJsonAsync<List<SolicitudPropiaRespuesta>>("/api/solicitudes-cliente/mias", Ct))!;
        Assert.Equal(2, lista.Count); Assert.Equal(new[] { "Cancelada", "Rechazada" }, lista.Select(s => s.Estado).Order());
        Assert.All(lista, s => Assert.StartsWith("PRUEBA", s.Descripcion));
    }

    [Theory]
    [InlineData("SinSesion", 401)] [InlineData("Administrador", 403)] [InlineData("Empleado", 403)]
    [InlineData("Pendiente", 403)] [InlineData("Suspendida", 401)] [InlineData("BajaLogica", 401)]
    public async Task Rutas_exigen_cliente_activo(string tipo, int codigo)
    {
        string? token = null;
        if (tipo == "Administrador") token = await datos.Token(app.CorreoAdmin, app.ClaveAdmin);
        else if (tipo != "SinSesion")
        {
            var c = await datos.Usuario(tipo == "Empleado" ? "Empleado" : "Cliente", tipo != "Pendiente"); token = c.Token;
            if (tipo is "Suspendida" or "BajaLogica") { using var db = app.NuevoContexto(); var u = await db.Usuarios.SingleAsync(u => u.IdUsuario == c.Id, Ct); u.EstadoCuenta = tipo; await db.SaveChangesAsync(Ct); }
        }
        using var h = datos.Http(token);
        foreach (var ruta in new[] { "mias", "tipos-bano", "1" })
            Assert.Equal((HttpStatusCode)codigo, (await h.GetAsync("/api/solicitudes-cliente/" + ruta, Ct)).StatusCode);
        Assert.Equal((HttpStatusCode)codigo, (await h.PostAsJsonAsync(Crear, Pedido(), Ct)).StatusCode);
    }

    [Fact]
    public async Task Borrador_app_conserva_clave_tras_rechazo_y_la_cambia_para_un_pedido_nuevo()
    {
        var c = await datos.Usuario("Cliente"); using var h = datos.Http(); var api = new ClienteApp.ClienteAutenticacion(h);
        var sesion = new ClienteApp.SesionUsuario(api, new AutenticacionAppPruebas.AlmacenPrueba { Token = c.Token });
        await sesion.InicializarAsync(); var primero = sesion.BorradorServicio();
        await Assert.ThrowsAsync<ClienteApp.ErrorApi>(() => sesion.SolicitarServicioAsync(primero));
        Assert.Same(primero, sesion.BorradorServicio());
        sesion.NuevoBorradorServicio(); Assert.NotEqual(primero.ClaveEnvio, sesion.BorradorServicio().ClaveEnvio);
        await sesion.CerrarAsync(); await Assert.ThrowsAsync<ClienteApp.ErrorApi>(() => sesion.MisSolicitudesAsync());
    }
}
