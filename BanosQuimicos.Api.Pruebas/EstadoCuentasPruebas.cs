using System.Net;
using System.Net.Http.Json;
using BanosQuimicos.Api.Contratos.Cuentas;
using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AppAuth = global::BanosQuimicos.App.Autenticacion;

namespace BanosQuimicos.Api.Pruebas;

public class EstadoCuentasPruebas(AplicacionTareasPruebas app) : IClassFixture<AplicacionTareasPruebas>
{
    private readonly TareasEmpleadoPruebas datos = new(app);
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private async Task<HttpClient> Admin() => datos.Http(await datos.Token(app.CorreoAdmin, app.ClaveAdmin));
    private Task<HttpResponseMessage> Estado(HttpClient a, int id, string anterior, string nuevo, string? motivo = null) =>
        a.PutAsJsonAsync($"/api/cuentas/{id}/estado", new { estadoAnterior = anterior, nuevoEstado = nuevo, motivo }, Ct);

    [Theory]
    [InlineData("Cliente")]
    [InlineData("Empleado")]
    public async Task Suspension_reactivacion_y_baja_conservan_datos_y_rechazan_sesion_anterior(string rol)
    {
        var c = await datos.Usuario(rol); using var a = await Admin(); using var http = datos.Http();
        var almacen = new AutenticacionAppPruebas.AlmacenPrueba { Token = c.Token };
        var sesion = new AppAuth.SesionUsuario(new AppAuth.ClienteAutenticacion(http), almacen);
        await sesion.InicializarAsync(); Assert.Equal(AppAuth.EstadoSesion.Activa, sesion.Estado);
        using var db = app.NuevoContexto(); var original = await db.Usuarios.AsNoTracking().SingleAsync(x => x.IdUsuario == c.Id, Ct);
        var perfil = await db.Clientes.AsNoTracking().SingleAsync(x => x.IdUsuario == c.Id, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, (await Estado(a, c.Id, "Activa", "Suspendida", "  ")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Estado(a, c.Id, "Activa", "Suspendida", " PRUEBA LOCAL: pausa ")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Estado(a, c.Id, "Activa", "Suspendida", "No sobrescribir")).StatusCode);
        await sesion.ActualizarAsync(); Assert.Equal(AppAuth.EstadoSesion.SinSesion, sesion.Estado); Assert.Null(almacen.Token);
        using var viejo = datos.Http(c.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await viejo.GetAsync("/api/servicios-cliente/mios", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/autenticacion/login", new { email = c.Email, contrasena = c.Clave }, Ct)).StatusCode);
        var detalle = (await a.GetFromJsonAsync<CuentaGestionRespuesta>($"/api/cuentas/{c.Id}", Ct))!;
        Assert.Equal("PRUEBA LOCAL: pausa", detalle.MotivoSuspension); Assert.Null(detalle.FechaBaja);
        Assert.Equal(HttpStatusCode.NoContent, (await Estado(a, c.Id, "Suspendida", "Activa")).StatusCode);
        await sesion.IngresarAsync(new() { Email = c.Email, Contrasena = c.Clave }); Assert.Equal(rol, sesion.Cuenta!.Rol);
        Assert.Equal(HttpStatusCode.NoContent, (await Estado(a, c.Id, "Activa", "BajaLogica")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Estado(a, c.Id, "Activa", "BajaLogica")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Estado(a, c.Id, "Suspendida", "Activa")).StatusCode);
        await sesion.ActualizarAsync(); Assert.Equal(AppAuth.EstadoSesion.SinSesion, sesion.Estado);
        var final = await db.Usuarios.AsNoTracking().SingleAsync(x => x.IdUsuario == c.Id, Ct);
        Assert.Equal(original.ContrasenaHash, final.ContrasenaHash); Assert.Equal(original.IdRol, final.IdRol);
        Assert.Equal("BajaLogica", final.EstadoCuenta); Assert.NotNull(final.FechaBaja);
        Assert.Equal(perfil.IdCliente, (await db.Clientes.SingleAsync(x => x.IdUsuario == c.Id, Ct)).IdCliente);
    }

    [Theory]
    [InlineData("Cliente")]
    [InlineData("Empleado")]
    public async Task Activa_cuentas_antiguas_pendientes_de_ambos_roles_sin_alterar_otras(string rol)
    {
        var u = await datos.Usuario(rol, false); var otra = await datos.Usuario("Cliente", false); using var a = await Admin();
        Assert.True((await a.GetFromJsonAsync<CuentaGestionRespuesta>($"/api/cuentas/{u.Id}", Ct))!.PuedeActivar);
        Assert.Equal(HttpStatusCode.NoContent, (await a.PostAsync($"/api/cuentas/{u.Id}/activar", null, Ct)).StatusCode);
        using var h = datos.Http(u.Token);
        var yo = (await h.GetFromJsonAsync<AppAuth.CuentaActual>("/api/autenticacion/yo", Ct))!;
        Assert.Equal(rol, yo.Rol); Assert.Equal("Activa", yo.EstadoCuenta);
        using var db = app.NuevoContexto(); Assert.Equal("Pendiente", (await db.Usuarios.SingleAsync(x => x.IdUsuario == otra.Id, Ct)).EstadoCuenta);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(7)]
    public async Task Empleado_con_trabajos_bloquea_transiciones_y_conserva_historial_y_responsable(int estado)
    {
        var e = await datos.Usuario(); var t = await datos.Tarea(e.Id, null, conServicio: false, estado: estado); using var a = await Admin();
        using (var db = app.NuevoContexto()) { await db.BanosQuimicos.Where(x => x.IdBano == t.IdBano).ExecuteUpdateAsync(x => x.SetProperty(b => b.IdResponsableLimpieza, e.Id), Ct); }
        foreach (var destino in new[] { "Suspendida", "BajaLogica" })
        {
            var r = await Estado(a, e.Id, "Activa", destino, destino == "Suspendida" ? "PRUEBA" : null);
            Assert.Equal(HttpStatusCode.Conflict, r.StatusCode); Assert.Contains($"Tarea {t.IdTarea}", await r.Content.ReadAsStringAsync(Ct));
        }
        Assert.Equal(HttpStatusCode.Conflict, (await a.PutAsJsonAsync($"/api/cuentas/{e.Id}/rol", new { rolAnterior = "Empleado", nuevoRol = "Cliente" }, Ct)).StatusCode);
        using var empleado = datos.Http(e.Token);
        Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{t.IdTarea}/registrar-trabajo", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Estado(a, e.Id, "Activa", "Suspendida", "PRUEBA")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{t.IdTarea}/finalizar", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Estado(a, e.Id, "Activa", "Suspendida", "PRUEBA")).StatusCode);
        var b = Assert.Single((await a.GetFromJsonAsync<List<BanosQuimicos.Api.Contratos.Banos.EstadoBanoRespuesta>>("/api/banos/estado", Ct))!, b => b.IdBano == t.IdBano);
        Assert.Equal(e.Id, b.IdResponsable); Assert.False(b.ResponsableActivo);
        Assert.DoesNotContain((await a.GetFromJsonAsync<List<BanosQuimicos.Api.Contratos.Banos.EmpleadoAsignableRespuesta>>("/api/banos/empleados-asignables", Ct))!, x => x.IdUsuario == e.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await a.PutAsJsonAsync($"/api/cuentas/{e.Id}/rol", new { rolAnterior = "Empleado", nuevoRol = "Cliente" }, Ct)).StatusCode);
        using var final = app.NuevoContexto(); Assert.Equal("Suspendida", (await final.Usuarios.SingleAsync(x => x.IdUsuario == e.Id, Ct)).EstadoCuenta);
        Assert.Equal(e.Id, (await final.TareasOperativas.SingleAsync(x => x.IdTarea == t.IdTarea, Ct)).IdEmpleadoRegistroTrabajo);
    }

    [Fact]
    public async Task Cliente_con_servicio_o_solicitud_abierta_no_se_deshabilita_ni_cambia_rol()
    {
        var e = await datos.Usuario(); var t = await datos.Tarea(e.Id, null); using var a = await Admin(); using var db = app.NuevoContexto();
        var s = await db.Servicios.Include(x => x.Cliente).SingleAsync(x => x.IdServicio == t.IdServicio, Ct); var id = s.Cliente.IdUsuario;
        var solicitud = new SolicitudCliente { IdCliente = s.IdCliente, IdTipoOperacion = 5, IdEstadoOperacion = 1,
            FechaSolicitud = AplicacionTareasPruebas.Ahora, IdServicioReferencia = s.IdServicio, Descripcion = "PRUEBA CONFLICTO CUENTA" };
        db.Add(solicitud); await db.SaveChangesAsync(Ct);
        foreach (var destino in new[] { "Suspendida", "BajaLogica" })
        {
            var r = await Estado(a, id, "Activa", destino, destino == "Suspendida" ? "PRUEBA" : null);
            Assert.Equal(HttpStatusCode.Conflict, r.StatusCode); var texto = await r.Content.ReadAsStringAsync(Ct);
            Assert.Contains($"Servicio {s.IdServicio}", texto); Assert.Contains($"Solicitud {solicitud.IdSolicitud}", texto);
        }
        Assert.Equal(HttpStatusCode.Conflict, (await a.PutAsJsonAsync($"/api/cuentas/{id}/rol", new { rolAnterior = "Cliente", nuevoRol = "Empleado" }, Ct)).StatusCode);
        Assert.Equal("Entregado", (await db.Servicios.AsNoTracking().SingleAsync(x => x.IdServicio == s.IdServicio, Ct)).EstadoServicio);
        Assert.Equal(1, (await db.SolicitudesCliente.AsNoTracking().SingleAsync(x => x.IdSolicitud == solicitud.IdSolicitud, Ct)).IdEstadoOperacion);
    }

    [Theory]
    [InlineData("Anonimo", 401)] [InlineData("Cliente", 403)] [InlineData("Empleado", 403)] [InlineData("Pendiente", 403)]
    public async Task Solo_administrador_activo_puede_cambiar_estado(string tipo, int codigo)
    {
        var c = await datos.Usuario("Cliente");
        using var actor = tipo == "Anonimo" ? datos.Http() : datos.Http((await datos.Usuario(tipo == "Empleado" ? "Empleado" : "Cliente", tipo != "Pendiente")).Token);
        Assert.Equal(codigo, (int)(await Estado(actor, c.Id, "Activa", "BajaLogica")).StatusCode);
    }

    [Fact]
    public async Task Administradores_no_pueden_deshabilitarse_a_si_mismos_y_concurrencia_conserva_uno_activo()
    {
        using var a = await Admin(); using var db = app.NuevoContexto();
        var propio = await db.Usuarios.Where(x => x.Email == app.CorreoAdmin).Select(x => x.IdUsuario).SingleAsync(Ct);
        foreach (var destino in new[] { "Suspendida", "BajaLogica" })
            Assert.Equal(HttpStatusCode.Conflict, (await Estado(a, propio, "Activa", destino, destino == "Suspendida" ? "PRUEBA" : null)).StatusCode);
        // Administradores adicionales únicamente en esta base aislada, sin crear un alta pública de ese rol.
        var b = await datos.Usuario("Cliente"); var c = await datos.Usuario("Cliente");
        var rol = await db.Roles.Where(x => x.NombreRol == "Administrador").Select(x => x.IdRol).SingleAsync(Ct);
        await db.Usuarios.Where(x => x.IdUsuario == b.Id || x.IdUsuario == c.Id).ExecuteUpdateAsync(x => x.SetProperty(u => u.IdRol, rol), Ct);
        using var hb = datos.Http(b.Token); using var hc = datos.Http(c.Token);
        var rs = await Task.WhenAll(Estado(hb, c.Id, "Activa", "Suspendida", "PRUEBA"), Estado(hc, b.Id, "Activa", "BajaLogica"));
        Assert.Single(rs, r => r.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(rs, r => r.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized);
        foreach (var r in rs) r.Dispose();
        Assert.True(await db.Usuarios.CountAsync(x => x.EstadoCuenta == "Activa" && x.IdRol == rol, Ct) >= 1);
    }

    [Fact]
    public async Task Suspension_y_asignacion_concurrentes_no_dejan_trabajo_nuevo_con_empleado_inactivo()
    {
        var e = await datos.Usuario(); using var a = await Admin(); using var db = app.NuevoContexto();
        var b = new BanoQuimico { CodigoInterno = "PRUEBA-EC-" + Guid.NewGuid().ToString("N")[..8], IdTipoBano = 1, IdEstadoBano = 2, FechaAlta = AplicacionTareasPruebas.Ahora };
        db.Add(b); await db.SaveChangesAsync(Ct);
        var rs = await Task.WhenAll(Estado(a, e.Id, "Activa", "Suspendida", "PRUEBA"), a.PostAsJsonAsync($"/api/banos/{b.IdBano}/mantenimientos",
            new { claveProgramacion = Guid.NewGuid(), idEmpleado = e.Id, fecha = new DateOnly(2030, 4, 4), descripcion = "PRUEBA CARRERA ESTADO", bloqueaAsignacion = true }, Ct));
        Assert.Single(rs, r => r.IsSuccessStatusCode); Assert.Single(rs, r => r.StatusCode == HttpStatusCode.Conflict);
        foreach (var r in rs) r.Dispose();
        var activa = (await db.Usuarios.AsNoTracking().SingleAsync(x => x.IdUsuario == e.Id, Ct)).EstadoCuenta == "Activa";
        Assert.Equal(activa, await db.TareasOperativas.AnyAsync(x => x.IdBano == b.IdBano, Ct));
    }
}
