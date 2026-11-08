using System.Net;
using System.Net.Http.Json;
using BanosQuimicos.Api.Contratos.Banos;
using BanosQuimicos.Api.Contratos.Tareas;
using BanosQuimicos.Api.Contratos.Solicitudes;
using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanosQuimicos.Api.Pruebas;

public class InventarioBanosPruebas(AplicacionTareasPruebas app) : IClassFixture<AplicacionTareasPruebas>
{
    private readonly TareasEmpleadoPruebas datos = new(app);
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private async Task<HttpClient> Admin() => datos.Http(await datos.Token(app.CorreoAdmin, app.ClaveAdmin));
    private async Task<EstadoBanoRespuesta> Alta(HttpClient a, int tipo = 1)
    {
        var r = await a.PostAsJsonAsync("/api/banos", new AltaBanoSolicitud { ClaveAlta = Guid.NewGuid(), IdTipoBano = tipo }, Ct);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<EstadoBanoRespuesta>(Ct))!;
    }
    private MantenimientoBanoSolicitud Pedido(int empleado, bool bloquea = true) => new() {
        ClaveProgramacion = Guid.NewGuid(), IdEmpleado = empleado, Fecha = new(2030, 4, 4),
        Descripcion = "PRUEBA AISLADA: revisar bisagras", BloqueaAsignacion = bloquea };

    [Fact]
    public async Task Condicion_se_combina_con_inventario_sin_confundir_apto_con_disponible()
    {
        using var a = await Admin(); var e = await datos.Usuario(); var ocupada = await datos.Tarea(e.Id, null);
        var libre = await Alta(a); var noApto = await Alta(a); var bajaApto = await Alta(a); var bajaNoApto = await Alta(a);
        foreach (var b in new[] { noApto, bajaNoApto })
            Assert.Equal(HttpStatusCode.OK, (await a.PutAsJsonAsync($"/api/banos/{b.IdBano}/condicion", new { anterior = "Apto", nueva = "No apto" }, Ct)).StatusCode);
        foreach (var b in new[] { bajaApto, bajaNoApto })
            Assert.Equal(HttpStatusCode.OK, (await a.PostAsync($"/api/banos/{b.IdBano}/baja-inventario", null, Ct)).StatusCode);
        var todos = (await a.GetFromJsonAsync<List<EstadoBanoRespuesta>>("/api/banos/estado?inventario=Todos", Ct))!;
        foreach (var inventario in new[] { "Activos", "Bajas", "Todos" })
            foreach (var condicion in new[] { "Todos", "Apto", "No apto" })
            {
                var ruta = $"/api/banos/estado?inventario={inventario}&condicion={Uri.EscapeDataString(condicion)}";
                var resultado = (await a.GetFromJsonAsync<List<EstadoBanoRespuesta>>(ruta, Ct))!;
                var esperados = todos.Where(b => (inventario == "Todos" || (inventario == "Bajas" ? b.FechaBaja != null : b.FechaBaja == null))
                    && (condicion == "Todos" || b.Condicion == condicion)).Select(b => b.IdBano);
                Assert.Equal(esperados, resultado.Select(b => b.IdBano));
            }
        var aptos = (await a.GetFromJsonAsync<List<EstadoBanoRespuesta>>("/api/banos/estado?condicion=Apto", Ct))!;
        Assert.False(Assert.Single(aptos, b => b.IdBano == ocupada.IdBano).Disponible);
        Assert.True(Assert.Single(aptos, b => b.IdBano == libre.IdBano).Disponible);
        Assert.Equal(HttpStatusCode.BadRequest, (await a.GetAsync("/api/banos/estado?condicion=Disponible", Ct)).StatusCode);
    }

    [Fact]
    public async Task Alta_concurrente_es_idempotente_y_nuevas_altas_generan_codigos_distintos()
    {
        using var a = await Admin(); var clave = Guid.NewGuid();
        var p = new AltaBanoSolicitud { ClaveAlta = clave, IdTipoBano = 1 };
        var rs = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => a.PostAsJsonAsync("/api/banos", p, Ct)));
        Assert.Single(rs, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(3, rs.Count(r => r.StatusCode == HttpStatusCode.OK));
        var bs = new List<EstadoBanoRespuesta>();
        foreach (var r in rs) { bs.Add((await r.Content.ReadFromJsonAsync<EstadoBanoRespuesta>(Ct))!); r.Dispose(); }
        var b = bs[0]; Assert.Single(bs.Select(x => x.IdBano).Distinct());
        Assert.StartsWith("B.O-", b.Codigo); Assert.Equal("Apto", b.Condicion); Assert.True(b.Disponible);
        Assert.Null(b.IdServicio); Assert.Null(b.IdResponsable); Assert.Null(b.FechaBaja);
        p.IdTipoBano = 2;
        Assert.Equal(HttpStatusCode.Conflict, (await a.PostAsJsonAsync("/api/banos", p, Ct)).StatusCode);
        var nuevos = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => Alta(a, 2)));
        Assert.Equal(3, nuevos.Select(x => x.Codigo).Distinct().Count());
        Assert.All(nuevos, x => Assert.StartsWith("B.E-", x.Codigo));
    }

    [Fact]
    public async Task Correlativo_incluye_bajas_no_tiene_limite_ilustrativo_y_base_impide_codigo_duplicado()
    {
        using var a = await Admin(); using var db = app.NuevoContexto();
        var historico = new BanoQuimico { CodigoInterno = "B.E-999", IdTipoBano = 2, IdEstadoBano = 1,
            FechaAlta = AplicacionTareasPruebas.Ahora.AddDays(-2), FechaBaja = AplicacionTareasPruebas.Ahora.AddDays(-1) };
        db.Add(historico); await db.SaveChangesAsync(Ct);
        var nuevo = await Alta(a, 2); Assert.Equal("B.E-1000", nuevo.Codigo);
        db.Add(new BanoQuimico { CodigoInterno = nuevo.Codigo, IdTipoBano = 2, IdEstadoBano = 1, FechaAlta = AplicacionTareasPruebas.Ahora });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(Ct));
    }

    [Theory]
    [InlineData("Anonimo", 401)]
    [InlineData("Cliente", 403)]
    [InlineData("Empleado", 403)]
    [InlineData("Pendiente", 403)]
    public async Task Operaciones_exclusivas_de_administrador_activo(string rol, int codigo)
    {
        using var h = rol == "Anonimo" ? datos.Http() : datos.Http((await datos.Usuario(rol == "Empleado" ? "Empleado" : "Cliente", rol != "Pendiente")).Token);
        var rs = new[] {
            await h.GetAsync("/api/banos/tipos", Ct),
            await h.PostAsJsonAsync("/api/banos", new AltaBanoSolicitud { IdTipoBano = 1, ClaveAlta = Guid.NewGuid() }, Ct),
            await h.PutAsJsonAsync("/api/banos/1/condicion", new CondicionBanoSolicitud { Anterior = "Apto", Nueva = "No apto" }, Ct),
            await h.PostAsync("/api/banos/1/baja-inventario", null, Ct),
            await h.PostAsJsonAsync("/api/banos/1/mantenimientos", Pedido(1), Ct) };
        foreach (var r in rs) { Assert.Equal(codigo, (int)r.StatusCode); r.Dispose(); }
    }

    [Fact]
    public async Task Condicion_no_cierra_servicio_ni_tareas_y_baja_explica_bloqueos()
    {
        var e = await datos.Usuario(); var t = await datos.Tarea(e.Id, null);
        using var a = await Admin(); var ruta = $"/api/banos/{t.IdBano}";
        var r = await a.PutAsJsonAsync(ruta + "/condicion", new CondicionBanoSolicitud { Anterior = "Apto", Nueva = "No apto" }, Ct);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var baja = await a.PostAsync(ruta + "/baja-inventario", null, Ct);
        Assert.Equal(HttpStatusCode.Conflict, baja.StatusCode);
        var texto = await baja.Content.ReadAsStringAsync(Ct);
        Assert.Contains($"Servicio {t.IdServicio}", texto); Assert.Contains($"Tarea {t.IdTarea}", texto);
        using var db = app.NuevoContexto();
        Assert.Equal("Entregado", (await db.Servicios.SingleAsync(s => s.IdServicio == t.IdServicio, Ct)).EstadoServicio);
        Assert.Equal(6, (await db.TareasOperativas.SingleAsync(x => x.IdTarea == t.IdTarea, Ct)).IdEstadoOperacion);
        Assert.Null((await db.BanosQuimicos.SingleAsync(b => b.IdBano == t.IdBano, Ct)).FechaBaja);
        Assert.Equal(HttpStatusCode.Conflict, (await a.PostAsJsonAsync(ruta + "/mantenimientos", Pedido(e.Id), Ct)).StatusCode);
    }

    [Fact]
    public async Task Mantenimiento_sin_servicio_se_conserva_se_cumple_y_no_cambia_condicion_ni_otros_bloqueos()
    {
        using var a = await Admin(); var e = await datos.Usuario(); var otro = await datos.Usuario(); var b = await Alta(a);
        var ruta = $"/api/banos/{b.IdBano}";
        Assert.Equal(HttpStatusCode.OK, (await a.PutAsJsonAsync(ruta + "/condicion", new CondicionBanoSolicitud { Anterior = "Apto", Nueva = "No apto" }, Ct)).StatusCode);
        var p = Pedido(e.Id); var rs = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => a.PostAsJsonAsync(ruta + "/mantenimientos", p, Ct)));
        Assert.Single(rs, r => r.StatusCode == HttpStatusCode.Created);
        var ids = new List<int>();
        foreach (var r in rs) { ids.Add((await r.Content.ReadFromJsonAsync<global::BanosQuimicos.App.Autenticacion.LimpiezaProgramada>(Ct))!.IdTarea); r.Dispose(); }
        Assert.Single(ids.Distinct()); var id = ids[0];
        p.Descripcion += " diferente";
        Assert.Equal(HttpStatusCode.Conflict, (await a.PostAsJsonAsync(ruta + "/mantenimientos", p, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await a.PostAsJsonAsync(ruta + "/mantenimientos", Pedido(e.Id), Ct)).StatusCode);
        var bloqueada = await a.PostAsync(ruta + "/baja-inventario", null, Ct);
        Assert.Equal(HttpStatusCode.Conflict, bloqueada.StatusCode); Assert.Contains($"Tarea {id}", await bloqueada.Content.ReadAsStringAsync(Ct));
        using var empleado = datos.Http(e.Token); using var ajeno = datos.Http(otro.Token);
        Assert.Equal(HttpStatusCode.NotFound, (await ajeno.PostAsync($"/api/tareas/{id}/registrar-trabajo", null, Ct)).StatusCode);
        foreach (var op in new[] { "registrar-trabajo", "finalizar" })
            Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{id}/{op}", null, Ct)).StatusCode);
        var detalle = (await empleado.GetFromJsonAsync<TareaAsignadaRespuesta>($"/api/tareas/{id}", Ct))!;
        Assert.Equal("No apto", detalle.CondicionBano); Assert.Equal(1, detalle.OtrosMantenimientosBloqueantes);
        Assert.Equal("PRUEBA AISLADA: revisar bisagras", detalle.DescripcionTrabajo);
        Assert.Null(detalle.ObservacionesEmpleado); Assert.Null(detalle.PapelHigienicoRepuesto);
        // Una sesión nueva recupera las referencias guardadas, sin depender de la pantalla abierta.
        using var nueva = datos.Http(await datos.Token(e.Email, e.Clave));
        Assert.Equal("Realizada", (await nueva.GetFromJsonAsync<TareaAsignadaRespuesta>($"/api/tareas/{id}", Ct))!.Estado);
        var apto = await a.PutAsJsonAsync(ruta + "/condicion", new CondicionBanoSolicitud { Anterior = "No apto", Nueva = "Apto" }, Ct);
        var estado = (await apto.Content.ReadFromJsonAsync<EstadoBanoRespuesta>(Ct))!;
        Assert.True(estado.MantenimientoBloqueante); Assert.False(estado.Disponible);
        using var db = app.NuevoContexto(); var guardada = await db.TareasOperativas.SingleAsync(t => t.IdTarea == id, Ct);
        Assert.Null(guardada.IdServicio); Assert.Null(guardada.IdSolicitud); Assert.Equal(e.Id, guardada.IdEmpleadoRegistroTrabajo);
    }

    [Fact]
    public async Task Baja_logica_conserva_historial_filtra_y_no_admite_nuevos_trabajos_ni_reactiva_por_condicion()
    {
        using var a = await Admin(); var e = await datos.Usuario(); var t = await datos.Tarea(e.Id, null, conServicio: false, estado: 9);
        var ruta = $"/api/banos/{t.IdBano}";
        var r = await a.PostAsync(ruta + "/baja-inventario", null, Ct); Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var baja = (await r.Content.ReadFromJsonAsync<EstadoBanoRespuesta>(Ct))!; Assert.NotNull(baja.FechaBaja); Assert.False(baja.Disponible);
        var repetida = await a.PostAsync(ruta + "/baja-inventario", null, Ct);
        Assert.Equal(baja.FechaBaja, (await repetida.Content.ReadFromJsonAsync<EstadoBanoRespuesta>(Ct))!.FechaBaja);
        Assert.DoesNotContain((await a.GetFromJsonAsync<List<EstadoBanoRespuesta>>("/api/banos/estado", Ct))!, x => x.IdBano == t.IdBano);
        Assert.Contains((await a.GetFromJsonAsync<List<EstadoBanoRespuesta>>("/api/banos/estado?inventario=Bajas", Ct))!, x => x.IdBano == t.IdBano);
        Assert.Equal(HttpStatusCode.Conflict, (await a.PostAsJsonAsync(ruta + "/mantenimientos", Pedido(e.Id), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await a.PutAsJsonAsync(ruta + "/responsable-limpieza", new { idEmpleado = e.Id }, Ct)).StatusCode);
        var apto = await a.PutAsJsonAsync(ruta + "/condicion", new CondicionBanoSolicitud { Anterior = "Apto", Nueva = "Apto" }, Ct);
        Assert.False((await apto.Content.ReadFromJsonAsync<EstadoBanoRespuesta>(Ct))!.Disponible);
        using var db = app.NuevoContexto(); Assert.Equal(9, (await db.TareasOperativas.SingleAsync(x => x.IdTarea == t.IdTarea, Ct)).IdEstadoOperacion);
        Assert.Equal(1, await db.BanosQuimicos.CountAsync(x => x.IdBano == t.IdBano, Ct));
    }

    [Fact]
    public async Task Baja_y_programacion_concurrentes_no_dejan_tarea_activa_en_bano_dado_de_baja()
    {
        using var a = await Admin(); var e = await datos.Usuario(); var b = await Alta(a); var ruta = $"/api/banos/{b.IdBano}";
        var rs = await Task.WhenAll(a.PostAsync(ruta + "/baja-inventario", null, Ct), a.PostAsJsonAsync(ruta + "/mantenimientos", Pedido(e.Id), Ct));
        Assert.Single(rs, r => r.IsSuccessStatusCode); Assert.Single(rs, r => r.StatusCode == HttpStatusCode.Conflict);
        foreach (var r in rs) r.Dispose();
        using var db = app.NuevoContexto(); var baja = (await db.BanosQuimicos.SingleAsync(x => x.IdBano == b.IdBano, Ct)).FechaBaja != null;
        var activas = await db.TareasOperativas.AnyAsync(t => t.IdBano == b.IdBano && (t.IdEstadoOperacion == 6 || t.IdEstadoOperacion == 7), Ct);
        Assert.NotEqual(baja, activas);
    }

    [Fact]
    public async Task Validaciones_rechazan_tipo_clave_descripcion_y_empleado_invalidos()
    {
        using var a = await Admin(); var b = await Alta(a); var cliente = await datos.Usuario("Cliente");
        Assert.Equal(HttpStatusCode.BadRequest, (await a.PostAsJsonAsync("/api/banos", new { idTipoBano = 1, claveAlta = Guid.Empty }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await a.PostAsJsonAsync("/api/banos", new { idTipoBano = 999, claveAlta = Guid.NewGuid() }, Ct)).StatusCode);
        var ruta = $"/api/banos/{b.IdBano}/mantenimientos"; var p = Pedido(cliente.Id);
        Assert.Equal(HttpStatusCode.Conflict, (await a.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
        p.Descripcion = "   "; Assert.Equal(HttpStatusCode.BadRequest, (await a.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
        p.Descripcion = new string('x', 2001); Assert.Equal(HttpStatusCode.BadRequest, (await a.PostAsJsonAsync(ruta, p, Ct)).StatusCode);
    }

    [Fact]
    public async Task Baja_compite_con_asignacion_de_servicio_y_banos_inactivos_no_se_ofrecen()
    {
        using var a = await Admin(); var e = await datos.Usuario(); var cliente = await datos.Usuario("Cliente");
        var b = await Alta(a); var bajaPrevia = await Alta(a);
        Assert.Equal(HttpStatusCode.OK, (await a.PostAsync($"/api/banos/{bajaPrevia.IdBano}/baja-inventario", null, Ct)).StatusCode);
        using var db = app.NuevoContexto();
        var s = new SolicitudCliente { IdCliente = await db.Clientes.Where(x => x.IdUsuario == cliente.Id).Select(x => x.IdCliente).SingleAsync(Ct),
            IdTipoOperacion = 1, IdEstadoOperacion = 1, FechaSolicitud = AplicacionTareasPruebas.Ahora,
            CantidadBanos = 1, IdTipoBanoSolicitado = 1, TipoUsoSolicitado = "Evento", DireccionSolicitada = "PRUEBA INVENTARIO 123",
            FechaDeseada = AplicacionTareasPruebas.Ahora };
        db.Add(s); await db.SaveChangesAsync(Ct);
        var ruta = $"/api/solicitudes/{s.IdSolicitud}";
        Assert.DoesNotContain((await a.GetFromJsonAsync<List<BanoDisponibleRespuesta>>(ruta + "/banos-disponibles", Ct))!, x => x.IdBano == bajaPrevia.IdBano);
        var pedido = new AsignarBanosSolicitud { ClaveProgramacion = Guid.NewGuid(), IdBanos = [bajaPrevia.IdBano],
            IdEmpleado = e.Id, IdEstadoAnterior = 1, CantidadLimpiezasSemana = 1, Fecha = new(2030, 4, 4) };
        Assert.Equal(HttpStatusCode.Conflict, (await a.PostAsJsonAsync(ruta + "/asignar-banos", pedido, Ct)).StatusCode);
        pedido.IdBanos = [b.IdBano]; pedido.ClaveProgramacion = Guid.NewGuid();
        var rs = await Task.WhenAll(a.PostAsync($"/api/banos/{b.IdBano}/baja-inventario", null, Ct), a.PostAsJsonAsync(ruta + "/asignar-banos", pedido, Ct));
        Assert.Single(rs, r => r.IsSuccessStatusCode); Assert.Single(rs, r => r.StatusCode == HttpStatusCode.Conflict);
        foreach (var r in rs) r.Dispose();
        var fueraInventario = (await db.BanosQuimicos.SingleAsync(x => x.IdBano == b.IdBano, Ct)).FechaBaja != null;
        var asignado = await db.Servicios.AnyAsync(x => x.IdBano == b.IdBano, Ct);
        Assert.NotEqual(fueraInventario, asignado);
    }

    [Fact]
    public async Task Baja_impide_tarea_en_curso_y_luego_preserva_servicio_y_tarea_historicos()
    {
        var e = await datos.Usuario(); var t = await datos.Tarea(e.Id, null, estado: 7); using var a = await Admin();
        // Preparación aislada: servicio ya finalizado, pero mantenimiento En curso del mismo baño.
        using (var db = app.NuevoContexto())
        {
            var s = await db.Servicios.SingleAsync(x => x.IdServicio == t.IdServicio, Ct);
            s.EstadoServicio = "Finalizado"; s.FechaRetiroReal = s.FechaFinalizacion = AplicacionTareasPruebas.Ahora;
            var tarea = await db.TareasOperativas.SingleAsync(x => x.IdTarea == t.IdTarea, Ct); tarea.IdTipoOperacion = 11;
            await db.SaveChangesAsync(Ct);
        }
        var ruta = $"/api/banos/{t.IdBano}/baja-inventario";
        Assert.Equal(HttpStatusCode.Conflict, (await a.PostAsync(ruta, null, Ct)).StatusCode);
        using var empleado = datos.Http(e.Token);
        foreach (var op in new[] { "registrar-trabajo", "finalizar" })
            Assert.Equal(HttpStatusCode.OK, (await empleado.PostAsync($"/api/tareas/{t.IdTarea}/{op}", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await a.PostAsync(ruta, null, Ct)).StatusCode);
        using var final = app.NuevoContexto();
        Assert.Equal("Finalizado", (await final.Servicios.SingleAsync(x => x.IdServicio == t.IdServicio, Ct)).EstadoServicio);
        Assert.Equal(8, (await final.TareasOperativas.SingleAsync(x => x.IdTarea == t.IdTarea, Ct)).IdEstadoOperacion);
        Assert.Equal(2, await final.UbicacionesServicio.CountAsync(x => x.IdServicio == t.IdServicio, Ct));
    }
}
