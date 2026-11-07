using System.Net;
using System.Net.Http.Json;
using BanosQuimicos.Api.Contratos.Solicitudes;
using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanosQuimicos.Api.Pruebas;

public class AsignarBanosServicioPruebas(AplicacionTareasPruebas app) : IClassFixture<AplicacionTareasPruebas>
{
    private readonly TareasEmpleadoPruebas datos = new(app);
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private async Task<HttpClient> Admin() => datos.Http(await datos.Token(app.CorreoAdmin, app.ClaveAdmin));
    private async Task<BanoQuimico> Bano(int tipo = 1, int condicion = 1)
    {
        using var db = app.NuevoContexto(); var b = new BanoQuimico { CodigoInterno = "PN-" + Guid.NewGuid().ToString("N")[..12],
            IdTipoBano = tipo, IdEstadoBano = condicion, FechaAlta = AplicacionTareasPruebas.Ahora.AddDays(-1) };
        db.BanosQuimicos.Add(b); await db.SaveChangesAsync(Ct); return b;
    }
    private async Task<SolicitudCliente> Solicitud(int cantidad = 2)
    {
        var cliente = await datos.Usuario("Cliente"); using var db = app.NuevoContexto();
        var s = new SolicitudCliente { IdCliente = await db.Clientes.Where(c => c.IdUsuario == cliente.Id).Select(c => c.IdCliente).SingleAsync(Ct),
            IdTipoOperacion = 1, IdEstadoOperacion = 1, FechaSolicitud = AplicacionTareasPruebas.Ahora,
            CantidadBanos = cantidad, IdTipoBanoSolicitado = 1, TipoUsoSolicitado = "Evento", DireccionSolicitada = "PRUEBA NUEVO SERVICIO 123",
            FechaDeseada = AplicacionTareasPruebas.Ahora, Descripcion = "PRUEBA NUEVO PEDIDO " + Guid.NewGuid().ToString("N") };
        db.SolicitudesCliente.Add(s); await db.SaveChangesAsync(Ct); return s;
    }
    private static AsignarBanosSolicitud Pedido(int empleado, params int[] banos) => new() {
        ClaveProgramacion = Guid.NewGuid(), IdBanos = banos.ToList(), IdEmpleado = empleado,
        IdEstadoAnterior = 1, CantidadLimpiezasSemana = 0, Fecha = new DateOnly(2030, 4, 4)
    };

    [Fact]
    public async Task Pedido_crea_dos_servicios_y_entregas_coherentes_y_resuelve_solo_al_completar_ambas()
    {
        var empleado = await datos.Usuario(); var otro = await datos.Usuario(); var s = await Solicitud();
        var a = await Bano(); var b = await Bano(); using var admin = await Admin();
        using (var db = app.NuevoContexto()) { await db.BanosQuimicos.Where(x => x.IdBano == a.IdBano).ExecuteUpdateAsync(x => x.SetProperty(b => b.IdResponsableLimpieza, otro.Id), Ct); }
        var p = Pedido(empleado.Id, b.IdBano, a.IdBano); var ruta = $"/api/solicitudes/{s.IdSolicitud}/asignar-banos";
        var respuesta = await admin.PostAsJsonAsync(ruta, p, Ct); Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var aprobada = (await respuesta.Content.ReadFromJsonAsync<SolicitudDetalleRespuesta>(Ct))!;
        Assert.Equal("Aprobada", aprobada.Estado); Assert.Null(aprobada.FechaResolucion);
        Assert.Equal(2, aprobada.Servicios!.Count); Assert.Equal(2, aprobada.Trabajos.Count);
        Assert.All(aprobada.Servicios, x => { Assert.Equal("Pendiente de entrega", x.Estado); Assert.Equal("Evento", x.TipoUso); Assert.Equal(s.DireccionSolicitada, x.Direccion); });
        using (var db = app.NuevoContexto())
        {
            var servicios = await db.Servicios.Where(x => x.IdSolicitudOrigen == s.IdSolicitud).ToListAsync(Ct);
            foreach (var servicio in servicios)
            {
                Assert.Equal(s.IdCliente, servicio.IdCliente); Assert.Null(servicio.FechaEntregaReal);
                Assert.Equal(0, servicio.CantidadLimpiezasSemana);
                var t = await db.TareasOperativas.SingleAsync(x => x.IdServicio == servicio.IdServicio, Ct);
                Assert.Equal(servicio.IdBano, t.IdBano); Assert.Equal(s.IdSolicitud, t.IdSolicitud); Assert.Equal(7, t.IdTipoOperacion); Assert.Equal(6, t.IdEstadoOperacion);
                Assert.Equal(empleado.Id, t.IdEmpleado); Assert.Null(t.PapelHigienicoRepuesto); Assert.Null(t.ObservacionesEmpleado);
                var ubicacion = await db.UbicacionesServicio.SingleAsync(x => x.IdUbicacion == t.IdUbicacion, Ct);
                Assert.False(ubicacion.EsActual); Assert.Null(ubicacion.FechaDesde); Assert.Equal(servicio.IdServicio, ubicacion.IdServicio);
            }
            Assert.Equal(otro.Id, (await db.BanosQuimicos.SingleAsync(x => x.IdBano == a.IdBano, Ct)).IdResponsableLimpieza);
        }
        var libres = (await admin.GetFromJsonAsync<List<BanoDisponibleRespuesta>>($"/api/solicitudes/{s.IdSolicitud}/banos-disponibles", Ct))!;
        Assert.DoesNotContain(libres, x => x.IdBano == a.IdBano || x.IdBano == b.IdBano);
        using var trabajador = datos.Http(empleado.Token); using var ajeno = datos.Http(otro.Token);
        Assert.Equal(HttpStatusCode.NotFound, (await ajeno.GetAsync($"/api/tareas/{aprobada.Trabajos[0].IdTarea}", Ct)).StatusCode);
        foreach (var t in aprobada.Trabajos)
        {
            foreach (var accion in new[] { "registrar-trabajo", "finalizar" }) Assert.Equal(HttpStatusCode.OK, (await trabajador.PostAsync($"/api/tareas/{t.IdTarea}/" + accion, null, Ct)).StatusCode);
            var actual = (await admin.GetFromJsonAsync<SolicitudDetalleRespuesta>($"/api/solicitudes/{s.IdSolicitud}", Ct))!;
            Assert.Equal(t == aprobada.Trabajos[0] ? "Aprobada" : "Resuelta", actual.Estado);
        }
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync(ruta, p, Ct)).StatusCode); // Reintento posterior al cumplimiento.
        using var final = app.NuevoContexto(); Assert.Equal(2, await final.Servicios.CountAsync(x => x.IdSolicitudOrigen == s.IdSolicitud, Ct));
        Assert.Equal(2, await final.UbicacionesServicio.CountAsync(x => x.Servicio.IdSolicitudOrigen == s.IdSolicitud && x.EsActual && x.FechaDesde == AplicacionTareasPruebas.Ahora, Ct));
    }

    [Fact]
    public async Task Dos_pedidos_simultaneos_con_bano_compartido_no_generan_asignacion_parcial()
    {
        var e = await datos.Usuario(); var s1 = await Solicitud(); var s2 = await Solicitud();
        var comun = await Bano(); var a = await Bano(); var b = await Bano(); using var admin = await Admin();
        var respuestas = await Task.WhenAll(admin.PostAsJsonAsync($"/api/solicitudes/{s1.IdSolicitud}/asignar-banos", Pedido(e.Id, comun.IdBano, a.IdBano), Ct),
            admin.PostAsJsonAsync($"/api/solicitudes/{s2.IdSolicitud}/asignar-banos", Pedido(e.Id, comun.IdBano, b.IdBano), Ct));
        Assert.Single(respuestas, x => x.StatusCode == HttpStatusCode.OK); Assert.Single(respuestas, x => x.StatusCode == HttpStatusCode.Conflict);
        foreach (var r in respuestas) r.Dispose();
        using var db = app.NuevoContexto();
        var solicitudes = await db.SolicitudesCliente.Where(x => x.IdSolicitud == s1.IdSolicitud || x.IdSolicitud == s2.IdSolicitud).ToListAsync(Ct);
        var perdedora = Assert.Single(solicitudes, x => x.IdEstadoOperacion == 1);
        Assert.False(await db.Servicios.AnyAsync(x => x.IdSolicitudOrigen == perdedora.IdSolicitud, Ct));
        Assert.False(await db.TareasOperativas.AnyAsync(x => x.IdSolicitud == perdedora.IdSolicitud, Ct));
        Assert.Equal(1, await db.Servicios.CountAsync(x => x.IdBano == comun.IdBano, Ct));
    }

    [Fact]
    public async Task Seleccion_valida_cantidad_distintos_tipo_condicion_ocupacion_y_mantenimiento()
    {
        var e = await datos.Usuario(); var s = await Solicitud(); var apto = await Bano(); var noApto = await Bano(condicion: 2);
        var otroTipo = await Bano(tipo: 2); var bloqueado = await Bano(); var ocupado = await Bano();
        using (var db = app.NuevoContexto())
        {
            db.TareasOperativas.Add(new TareaOperativa { IdBano = bloqueado.IdBano, IdTipoOperacion = 11, IdEstadoOperacion = 6, BloqueaAsignacion = true, FechaRegistro = AplicacionTareasPruebas.Ahora });
            db.Servicios.Add(new Servicio { IdBano = ocupado.IdBano, IdCliente = s.IdCliente, TipoUso = "Obra", EstadoServicio = "Pendiente de entrega", FechaAsignacion = AplicacionTareasPruebas.Ahora });
            // Una limpieza/mantenimiento pendiente no bloqueante por sí solo no quita disponibilidad.
            db.TareasOperativas.Add(new TareaOperativa { IdBano = apto.IdBano, IdTipoOperacion = 11, IdEstadoOperacion = 6, FechaRegistro = AplicacionTareasPruebas.Ahora });
            await db.SaveChangesAsync(Ct);
        }
        using var admin = await Admin(); var ruta = $"/api/solicitudes/{s.IdSolicitud}/asignar-banos";
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(ruta, Pedido(e.Id, apto.IdBano), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(ruta, Pedido(e.Id, apto.IdBano, apto.IdBano), Ct)).StatusCode);
        foreach (var id in new[] { noApto.IdBano, otroTipo.IdBano, bloqueado.IdBano, ocupado.IdBano, int.MaxValue })
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, Pedido(e.Id, apto.IdBano, id), Ct)).StatusCode);
        var disponibles = (await admin.GetFromJsonAsync<List<BanoDisponibleRespuesta>>($"/api/solicitudes/{s.IdSolicitud}/banos-disponibles", Ct))!;
        Assert.Contains(disponibles, x => x.IdBano == apto.IdBano);
        Assert.DoesNotContain(disponibles, x => new[] { noApto.IdBano, otroTipo.IdBano, bloqueado.IdBano, ocupado.IdBano }.Contains(x.IdBano));
        using var final = app.NuevoContexto(); Assert.False(await final.Servicios.AnyAsync(x => x.IdSolicitudOrigen == s.IdSolicitud, Ct));
    }

    [Fact]
    public async Task Reintentos_concurrentes_conservan_servicios_entregas_y_claves_sin_duplicar()
    {
        var e = await datos.Usuario(); var s = await Solicitud(); var a = await Bano(); var b = await Bano(); using var admin = await Admin();
        var p = Pedido(e.Id, a.IdBano, b.IdBano); var ruta = $"/api/solicitudes/{s.IdSolicitud}/asignar-banos";
        var respuestas = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => admin.PostAsJsonAsync(ruta, p, Ct)));
        Assert.All(respuestas, x => Assert.Equal(HttpStatusCode.OK, x.StatusCode)); foreach (var r in respuestas) r.Dispose();
        p.CantidadLimpiezasSemana = 3; Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
        p.CantidadLimpiezasSemana = 0; p.ClaveProgramacion = Guid.NewGuid(); Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
        using var db = app.NuevoContexto();
        Assert.Equal(2, await db.Servicios.CountAsync(x => x.IdSolicitudOrigen == s.IdSolicitud, Ct));
        Assert.Equal(2, await db.TareasOperativas.CountAsync(x => x.IdSolicitud == s.IdSolicitud, Ct));
        Assert.Equal(2, await db.UbicacionesServicio.CountAsync(x => x.Servicio.IdSolicitudOrigen == s.IdSolicitud, Ct));
    }

    [Theory]
    [InlineData("Anonimo", 401)] [InlineData("Cliente", 403)] [InlineData("Empleado", 403)]
    public async Task Rutas_de_seleccion_y_confirmacion_son_solo_administrativas(string rol, int codigo)
    {
        var s = await Solicitud(1); string? token = rol == "Anonimo" ? null : (await datos.Usuario(rol)).Token;
        using var http = datos.Http(token);
        Assert.Equal((HttpStatusCode)codigo, (await http.GetAsync($"/api/solicitudes/{s.IdSolicitud}/banos-disponibles", Ct)).StatusCode);
        Assert.Equal((HttpStatusCode)codigo, (await http.PostAsJsonAsync($"/api/solicitudes/{s.IdSolicitud}/asignar-banos", Pedido(1, 1), Ct)).StatusCode);
    }

    [Fact]
    public async Task Datos_obligatorios_y_empleado_actual_se_validan_antes_de_crear_servicios()
    {
        var e = await datos.Usuario(); var s = await Solicitud(); var a = await Bano(); var b = await Bano(); using var admin = await Admin();
        var p = Pedido(e.Id, a.IdBano, b.IdBano); var ruta = $"/api/solicitudes/{s.IdSolicitud}/asignar-banos";
        p.CantidadLimpiezasSemana = null; Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
        p.CantidadLimpiezasSemana = 0;
        using (var db = app.NuevoContexto()) { await db.Usuarios.Where(x => x.IdUsuario == e.Id).ExecuteUpdateAsync(x => x.SetProperty(u => u.EstadoCuenta, "Suspendida"), Ct); }
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
        var otro = await datos.Usuario(); p.IdEmpleado = otro.Id;
        using (var db = app.NuevoContexto()) { await db.SolicitudesCliente.Where(x => x.IdSolicitud == s.IdSolicitud).ExecuteUpdateAsync(x => x.SetProperty(r => r.DireccionSolicitada, (string?)null), Ct); }
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
        using var final = app.NuevoContexto(); Assert.False(await final.Servicios.AnyAsync(x => x.IdSolicitudOrigen == s.IdSolicitud, Ct));
        Assert.Equal(1, (await final.SolicitudesCliente.SingleAsync(x => x.IdSolicitud == s.IdSolicitud, Ct)).IdEstadoOperacion);
    }

    [Fact]
    public async Task Historial_parcial_no_se_sobrescribe_para_completar_un_pedido()
    {
        var e = await datos.Usuario(); var s = await Solicitud(); var a = await Bano(); var b = await Bano();
        using (var db = app.NuevoContexto())
        {
            db.Servicios.Add(new Servicio { IdSolicitudOrigen = s.IdSolicitud, IdCliente = s.IdCliente, IdBano = a.IdBano,
                TipoUso = "Evento", EstadoServicio = "Pendiente de entrega", FechaAsignacion = AplicacionTareasPruebas.Ahora });
            await db.SaveChangesAsync(Ct);
        }
        using var admin = await Admin();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/solicitudes/{s.IdSolicitud}/asignar-banos", Pedido(e.Id, a.IdBano, b.IdBano), Ct)).StatusCode);
        using var final = app.NuevoContexto(); Assert.Equal(1, await final.Servicios.CountAsync(x => x.IdSolicitudOrigen == s.IdSolicitud, Ct));
        Assert.False(await final.TareasOperativas.AnyAsync(x => x.IdSolicitud == s.IdSolicitud, Ct));
    }
}
