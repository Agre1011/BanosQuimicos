using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using BanosQuimicos.Api.Contratos;
using BanosQuimicos.Api.Contratos.Tareas;
using BanosQuimicos.Api.Modelos;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using AppAuth = global::BanosQuimicos.App.Autenticacion;

namespace BanosQuimicos.Api.Pruebas;

public class PaginacionPruebas(AplicacionTareasPruebas app) : IClassFixture<AplicacionTareasPruebas>
{
    private readonly TareasEmpleadoPruebas datos = new(app);
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private async Task<HttpClient> Admin() => datos.Http(await datos.Token(app.CorreoAdmin, app.ClaveAdmin));
    private async Task<List<TareaOperativa>> Tareas(int empleado, int cantidad, int estado = 6)
    {
        using var db = app.NuevoContexto(); var b = new BanoQuimico { CodigoInterno = "PP-" + Guid.NewGuid().ToString("N")[..12],
            IdTipoBano = 1, IdEstadoBano = 1, FechaAlta = AplicacionTareasPruebas.Ahora.AddDays(-2) };
        var tareas = Enumerable.Range(0, cantidad).Select(i => new TareaOperativa { Bano = b, IdEmpleado = empleado, IdTipoOperacion = 11,
            IdEstadoOperacion = estado, FechaRegistro = AplicacionTareasPruebas.Ahora.AddDays(-1), FechaProgramada = AplicacionTareasPruebas.Ahora,
            FechaRealizada = estado == 8 ? AplicacionTareasPruebas.Ahora : null, FechaCancelacion = estado == 9 ? AplicacionTareasPruebas.Ahora : null }).ToList();
        db.AddRange(tareas); await db.SaveChangesAsync(Ct); return tareas;
    }

    [Fact]
    public async Task Contador_hoy_excluye_finalizadas_canceladas_y_registrar_no_descuenta()
    {
        var e = await datos.Usuario(); var otro = await datos.Usuario(); var pendientes = await Tareas(e.Id, 12);
        await Tareas(e.Id, 2, 7); await Tareas(e.Id, 3, 8); await Tareas(e.Id, 2, 9); await Tareas(otro.Id, 5);
        var fuera = await Tareas(e.Id, 1);
        using (var db = app.NuevoContexto()) await db.TareasOperativas.Where(t => t.IdTarea == fuera[0].IdTarea)
            .ExecuteUpdateAsync(t => t.SetProperty(x => x.FechaProgramada, AplicacionTareasPruebas.Ahora.AddDays(-1)), Ct);
        using var h = datos.Http(e.Token);
        async Task<int> Cuenta() => (await h.GetFromJsonAsync<ResumenTareasRespuesta>("/api/tareas/mias/resumen", Ct))!.Cantidad;
        Assert.Equal(14, await Cuenta());
        Assert.Equal(HttpStatusCode.OK, (await h.PostAsync($"/api/tareas/{pendientes[0].IdTarea}/registrar-trabajo", null, Ct)).StatusCode);
        Assert.Equal(14, await Cuenta());
        Assert.Equal(HttpStatusCode.OK, (await h.PostAsync($"/api/tareas/{pendientes[0].IdTarea}/finalizar", null, Ct)).StatusCode);
        Assert.Equal(13, await Cuenta());
        Assert.Equal(10, (await h.GetFromJsonAsync<PaginaRespuesta<JsonElement>>("/api/tareas/mias/pagina", Ct))!.Elementos.Count);
        Assert.Equal(13, await Cuenta()); // No depende de los diez elementos recibidos.
    }

    [Fact]
    public async Task Filtro_de_tareas_se_aplica_antes_de_paginar_y_respeta_propietario_y_registro_sin_cierre()
    {
        var e = await datos.Usuario(); var otro = await datos.Usuario(); await Tareas(e.Id, 12, 8);
        var pendiente = await Tareas(e.Id, 2); await Tareas(e.Id, 1, 7); await Tareas(e.Id, 1, 9); await Tareas(otro.Id, 13);
        using (var db = app.NuevoContexto()) { var t = await db.TareasOperativas.SingleAsync(t => t.IdTarea == pendiente[0].IdTarea, Ct);
            t.FechaTrabajoRegistrado = AplicacionTareasPruebas.Ahora; t.IdEmpleadoRegistroTrabajo = e.Id; await db.SaveChangesAsync(Ct); }
        using var h = datos.Http(e.Token);
        var sin = (await h.GetFromJsonAsync<PaginaRespuesta<JsonElement>>("/api/tareas/mias/pagina", Ct))!;
        Assert.Equal(3, sin.Total); Assert.Equal(3, sin.Elementos.Count); Assert.Contains(sin.Elementos, t => t.GetProperty("idTarea").GetInt32() == pendiente[0].IdTarea);
        var hechas = (await h.GetFromJsonAsync<PaginaRespuesta<JsonElement>>("/api/tareas/mias/pagina?estado=Realizadas&pagina=2", Ct))!;
        Assert.Equal(12, hechas.Total); Assert.Equal(2, hechas.Elementos.Count); Assert.All(hechas.Elementos, t => Assert.Equal("Realizada", t.GetProperty("estado").GetString()));
        var todas = (await h.GetFromJsonAsync<PaginaRespuesta<JsonElement>>("/api/tareas/mias/pagina?estado=Todas", Ct))!; Assert.Equal(16, todas.Total);
        Assert.Equal(HttpStatusCode.BadRequest, (await h.GetAsync("/api/tareas/mias/pagina?estado=Canceladas", Ct)).StatusCode);
        using var ajeno = datos.Http(otro.Token); Assert.Equal(HttpStatusCode.NotFound, (await ajeno.GetAsync($"/api/tareas/{pendiente[0].IdTarea}", Ct)).StatusCode);
    }

    private async Task<(string Cliente, string Empleado)> Listados()
    {
        var c = await datos.Usuario("Cliente"); var e = await datos.Usuario(); using var db = app.NuevoContexto();
        var cliente = await db.Clientes.SingleAsync(x => x.IdUsuario == c.Id, Ct); var marca = Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 23; i++)
        {
            var b = new BanoQuimico { CodigoInterno = $"P-{marca}-{i:00}", IdTipoBano = 1, IdEstadoBano = 1, FechaAlta = AplicacionTareasPruebas.Ahora.AddDays(-2) };
            var s = new Servicio { Cliente = cliente, Bano = b, TipoUso = "Obra", EstadoServicio = "Entregado", FechaAsignacion = AplicacionTareasPruebas.Ahora.AddDays(-1), FechaEntregaReal = AplicacionTareasPruebas.Ahora };
            var solicitud = new SolicitudCliente { Cliente = cliente, IdTipoOperacion = 5, IdEstadoOperacion = 1,
                FechaSolicitud = AplicacionTareasPruebas.Ahora, Descripcion = $"PRUEBA PAGINACION {marca} {i:00}", ServicioReferencia = s };
            db.Add(new UbicacionServicio { Servicio = s, Direccion = "PRUEBA PAGINACION 123", EsActual = true, FechaRegistro = AplicacionTareasPruebas.Ahora, FechaDesde = AplicacionTareasPruebas.Ahora });
            db.Add(solicitud);
        }
        await db.SaveChangesAsync(Ct); await Tareas(e.Id, 23); return (c.Token, e.Token);
    }

    [Theory]
    [InlineData("/api/tareas/mias", "/api/tareas/mias/pagina", "idTarea", "Empleado")]
    [InlineData("/api/banos/estado", "/api/banos/estado/pagina", "idBano", "Administrador")]
    [InlineData("/api/servicios", "/api/servicios/pagina", "idServicio", "Administrador")]
    [InlineData("/api/solicitudes", "/api/solicitudes/pagina", "idSolicitud", "Administrador")]
    [InlineData("/api/servicios-cliente/mios", "/api/servicios-cliente/mios/pagina", "idServicio", "Cliente")]
    [InlineData("/api/solicitudes-cliente/mias", "/api/solicitudes-cliente/mias/pagina", "idSolicitud", "Cliente")]
    public async Task Seis_listados_paginan_diez_con_orden_estable_total_completo_y_ajuste_de_pagina(string ruta, string paginada, string id, string rol)
    {
        var c = await Listados(); using var h = rol == "Administrador" ? await Admin() : datos.Http(rol == "Empleado" ? c.Empleado : c.Cliente);
        var originales = (await h.GetFromJsonAsync<List<JsonElement>>(ruta, Ct))!.Select(x => x.GetProperty(id).GetInt32()).ToList();
        var recibidos = new List<int>();
        for (int pagina = 1; pagina <= (originales.Count + 9) / 10; pagina++)
        {
            var p = (await h.GetFromJsonAsync<PaginaRespuesta<JsonElement>>(paginada + "?pagina=" + pagina, Ct))!;
            Assert.Equal(originales.Count, p.Total); Assert.Equal(10, p.TamanoPagina); Assert.Equal(pagina, p.Pagina);
            Assert.Equal(originales.Skip((pagina - 1) * 10).Take(10), p.Elementos.Select(x => x.GetProperty(id).GetInt32()));
            recibidos.AddRange(p.Elementos.Select(x => x.GetProperty(id).GetInt32()));
        }
        Assert.Equal(originales, recibidos); Assert.Equal(recibidos.Count, recibidos.Distinct().Count());
        var ultima = (await h.GetFromJsonAsync<PaginaRespuesta<JsonElement>>(paginada + "?pagina=2147483647", Ct))!;
        Assert.Equal((originales.Count + 9) / 10, ultima.Pagina);
        Assert.Equal(HttpStatusCode.BadRequest, (await h.GetAsync(paginada + "?pagina=0", Ct)).StatusCode);
        using var anonimo = datos.Http(); Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(paginada, Ct)).StatusCode);
        var otro = await datos.Usuario(rol == "Cliente" ? "Empleado" : "Cliente"); using var sinPermiso = datos.Http(otro.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await sinPermiso.GetAsync(paginada, Ct)).StatusCode);
        if (rol == "Cliente")
        {
            var otroCliente = await datos.Usuario("Cliente"); using var ajeno = datos.Http(otroCliente.Token);
            var vacia = (await ajeno.GetFromJsonAsync<PaginaRespuesta<JsonElement>>(paginada + "?pagina=99&idCliente=1", Ct))!;
            Assert.Equal(0, vacia.Total); Assert.Equal(1, vacia.Pagina); Assert.Empty(vacia.Elementos);
            var resumen = await h.GetFromJsonAsync<JsonElement>("/api/servicios-cliente/mios/resumen", Ct);
            Assert.Equal(23, resumen.GetProperty("cantidad").GetInt32());
        }
    }

    [Fact]
    public async Task Inventario_filtra_condicion_y_bajas_antes_de_paginar_sin_confundir_disponibilidad()
    {
        var c = await Listados(); using var a = await Admin();
        using (var db = app.NuevoContexto())
        {
            for (var i = 0; i < 12; i++) db.Add(new BanoQuimico { CodigoInterno = "ZP-" + Guid.NewGuid().ToString("N")[..12], IdTipoBano = 1, IdEstadoBano = 2,
                FechaAlta = AplicacionTareasPruebas.Ahora.AddDays(-2), FechaBaja = AplicacionTareasPruebas.Ahora });
            await db.SaveChangesAsync(Ct);
        }
        var ruta = "/api/banos/estado/pagina?inventario=Bajas&condicion=No%20apto&pagina=2";
        var p = (await a.GetFromJsonAsync<PaginaRespuesta<JsonElement>>(ruta, Ct))!;
        Assert.Equal(12, p.Total); Assert.Equal(2, p.Elementos.Count);
        Assert.All(p.Elementos, b => { Assert.Equal("No apto", b.GetProperty("condicion").GetString()); Assert.False(b.GetProperty("disponible").GetBoolean()); });
        var aptos = (await a.GetFromJsonAsync<PaginaRespuesta<JsonElement>>("/api/banos/estado/pagina?condicion=Apto", Ct))!;
        Assert.Contains(aptos.Elementos, b => !b.GetProperty("disponible").GetBoolean());
        using var http = datos.Http();
        var sesion = new AppAuth.SesionUsuario(new AppAuth.ClienteAutenticacion(http), new AutenticacionAppPruebas.AlmacenPrueba {
            Token = await datos.Token(app.CorreoAdmin, app.ClaveAdmin) });
        await sesion.InicializarAsync(); var captura = new Captura();
        await using var sp = new ServiceCollection().AddLogging().AddSingleton(sesion).AddSingleton<IComponentActivator>(captura).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(sp, sp.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.RenderComponentAsync<global::BanosQuimicos.App.Components.Administracion.EstadoBanos>();
            var vista = captura.Instancias[typeof(global::BanosQuimicos.App.Components.Administracion.EstadoBanos)];
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            async Task Invocar(string nombre, params object[] args) => await (Task)vista.GetType().GetMethod(nombre, flags)!.Invoke(vista, args)!;
            await Invocar("PaginarAsync", 2);
            vista.GetType().GetField("filtro", flags)!.SetValue(vista, "Bajas");
            vista.GetType().GetField("condicion", flags)!.SetValue(vista, "No apto");
            await Invocar("FiltrarAsync");
            Assert.Equal(1, vista.GetType().GetField("pagina", flags)!.GetValue(vista));
            Assert.Equal(12, vista.GetType().GetField("total", flags)!.GetValue(vista));
            await Invocar("PaginarAsync", 2); await Invocar("CargarAsync");
            Assert.Equal(2, vista.GetType().GetField("pagina", flags)!.GetValue(vista));
            Assert.Equal("Bajas", vista.GetType().GetField("filtro", flags)!.GetValue(vista));
            Assert.Equal("No apto", vista.GetType().GetField("condicion", flags)!.GetValue(vista));
        });
    }

    private sealed class Captura : IComponentActivator
    {
        public readonly Dictionary<Type, IComponent> Instancias = [];
        public IComponent CreateInstance(Type t) { var c = (IComponent)Activator.CreateInstance(t)!; Instancias[t] = c; return c; }
    }

    [Fact]
    public async Task Servicios_filtra_tipo_fisico_no_uso_ni_codigo_antes_de_paginar_y_conserva_seleccion()
    {
        using var a = await Admin(); var c = await datos.Usuario("Cliente"); using var db = app.NuevoContexto();
        var idCliente = await db.Clientes.Where(x => x.IdUsuario == c.Id).Select(x => x.IdCliente).SingleAsync(Ct);
        // Mismo endpoint que Agregar baño, en la base aislada: también deben funcionar las altas reales de la app.
        for (var i = 0; i < 24; i++)
        {
            var tipo = i % 2 + 1;
            var alta = await a.PostAsJsonAsync("/api/banos", new { claveAlta = Guid.NewGuid(), idTipoBano = tipo }, Ct);
            Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
            var b = (await alta.Content.ReadFromJsonAsync<BanosQuimicos.Api.Contratos.Banos.EstadoBanoRespuesta>(Ct))!;
            var servicio = new Servicio { IdCliente = idCliente, IdBano = b.IdBano, TipoUso = tipo == 1 ? "Evento" : "Obra",
                EstadoServicio = "Entregado", FechaAsignacion = AplicacionTareasPruebas.Ahora.AddDays(-1), FechaEntregaReal = AplicacionTareasPruebas.Ahora };
            db.Add(new UbicacionServicio { Servicio = servicio, Direccion = "PRUEBA FILTRO TIPO FISICO", EsActual = true,
                FechaRegistro = AplicacionTareasPruebas.Ahora, FechaDesde = AplicacionTareasPruebas.Ahora });
        }
        // Un código histórico sin prefijo oficial sigue clasificándose por el catálogo físico.
        db.Add(new Servicio { IdCliente = idCliente, Bano = new BanoQuimico { CodigoInterno = "LEG-" + Guid.NewGuid().ToString("N")[..12],
            IdTipoBano = 1, IdEstadoBano = 1, FechaAlta = AplicacionTareasPruebas.Ahora }, TipoUso = "Evento",
            EstadoServicio = "Entregado", FechaAsignacion = AplicacionTareasPruebas.Ahora, FechaEntregaReal = AplicacionTareasPruebas.Ahora });
        await db.SaveChangesAsync(Ct);
        foreach (var (tipo, cantidad) in new[] { ("Obra", 13), ("Evento", 12), ("Todos", 25) })
        {
            var p = (await a.GetFromJsonAsync<PaginaRespuesta<JsonElement>>($"/api/servicios/pagina?tipoBano={tipo}&pagina=2", Ct))!;
            Assert.Equal(cantidad, p.Total); Assert.Equal(Math.Min(10, cantidad - 10), p.Elementos.Count);
            if (tipo != "Todos") Assert.All(p.Elementos, s => {
                Assert.Equal(tipo, s.GetProperty("tipoBano").GetString());
                Assert.NotEqual(tipo, s.GetProperty("tipoUso").GetString());
            });
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await a.GetAsync("/api/servicios/pagina?tipoBano=Inexistente", Ct)).StatusCode);
        using var http = datos.Http(); var sesion = new AppAuth.SesionUsuario(new AppAuth.ClienteAutenticacion(http),
            new AutenticacionAppPruebas.AlmacenPrueba { Token = await datos.Token(app.CorreoAdmin, app.ClaveAdmin) });
        await sesion.InicializarAsync(); var captura = new Captura();
        await using var sp = new ServiceCollection().AddLogging().AddSingleton(sesion).AddSingleton<IComponentActivator>(captura).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(sp, sp.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await renderer.RenderComponentAsync<global::BanosQuimicos.App.Components.Administracion.BanosServicios>();
            var vista = captura.Instancias[typeof(global::BanosQuimicos.App.Components.Administracion.BanosServicios)];
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            async Task Invocar(string n, params object[] args) => await (Task)vista.GetType().GetMethod(n, flags)!.Invoke(vista, args)!;
            await Invocar("PaginarAsync", 2); vista.GetType().GetField("tipoBano", flags)!.SetValue(vista, "Obra");
            await Invocar("FiltrarAsync"); Assert.Equal(1, vista.GetType().GetField("pagina", flags)!.GetValue(vista));
            await Invocar("PaginarAsync", 2);
            var servicios = (List<AppAuth.ServicioGestion>)vista.GetType().GetField("servicios", flags)!.GetValue(vista)!;
            var candidato = servicios.FirstOrDefault(s => s.IdUbicacionActual != null);
            if (candidato is not null)
            {
                vista.GetType().GetMethod("AbrirRetiro", flags)!.Invoke(vista, [candidato]);
                vista.GetType().GetMethod("CerrarSeleccion", flags)!.Invoke(vista, null);
            }
            await Invocar("CargarAsync");
            Assert.Equal(2, vista.GetType().GetField("pagina", flags)!.GetValue(vista));
            Assert.Equal("Obra", vista.GetType().GetField("tipoBano", flags)!.GetValue(vista));
        });
        // El servicio no recibe nunca una instrucción de borrar o cambiar datos para filtrar.
        Assert.Equal(25, await db.Servicios.CountAsync(s => s.IdCliente == idCliente, Ct));
    }

    [Fact]
    public async Task Marca_solo_en_login_registro_e_inicios_no_en_pantallas_hijas()
    {
        var empleado = await datos.Usuario(); var cliente = await datos.Usuario("Cliente");
        foreach (var (token, rol, campos) in new[] {
            ((string?)null, "Acceso", new[] { "registro" }),
            ((string?)await datos.Token(app.CorreoAdmin, app.ClaveAdmin), "Administrador", new[] { "gestion", "verBanos", "verSolicitudes", "verServicios" }),
            ((string?)empleado.Token, "Empleado", new[] { "verTareas" }),
            ((string?)cliente.Token, "Cliente", new[] { "verMisServicios", "verMisSolicitudes", "nuevoServicio" }) })
        {
            using var http = datos.Http(); var api = new AppAuth.ClienteAutenticacion(http);
            var sesion = new AppAuth.SesionUsuario(api, new AutenticacionAppPruebas.AlmacenPrueba { Token = token });
            await sesion.InicializarAsync(); var captura = new Captura();
            await using var sp = new ServiceCollection().AddLogging().AddSingleton(api).AddSingleton(sesion).AddSingleton<IComponentActivator>(captura).BuildServiceProvider();
            await using var renderer = new HtmlRenderer(sp, sp.GetRequiredService<ILoggerFactory>());
            await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var html = await renderer.RenderComponentAsync<global::BanosQuimicos.App.Components.Pages.Home>();
                Assert.Contains("alt=\"Celeris\"", html.ToHtmlString());
                var inicio = captura.Instancias[typeof(global::BanosQuimicos.App.Components.Pages.Home)];
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                foreach (var campo in campos)
                {
                    inicio.GetType().GetField(campo, flags)!.SetValue(inicio, true);
                    Assert.Equal(rol == "Acceso", (bool)inicio.GetType().GetProperty("MostrarMarca", flags)!.GetValue(inicio)!);
                    inicio.GetType().GetField(campo, flags)!.SetValue(inicio, false);
                }
                Assert.True((bool)inicio.GetType().GetProperty("MostrarMarca", flags)!.GetValue(inicio)!);
            });
        }
    }

    [Fact]
    public async Task Navegacion_conserva_filtro_y_pagina_ajusta_ultima_y_inicio_recupera_contador_actual()
    {
        var e = await datos.Usuario(); var tareas = await Tareas(e.Id, 11); using var h = datos.Http();
        var sesion = new AppAuth.SesionUsuario(new AppAuth.ClienteAutenticacion(h), new AutenticacionAppPruebas.AlmacenPrueba { Token = e.Token });
        await sesion.InicializarAsync(); var captura = new Captura();
        await using var sp = new ServiceCollection().AddLogging().AddSingleton(sesion).AddSingleton<IComponentActivator>(captura).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(sp, sp.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var vista = await renderer.RenderComponentAsync<global::BanosQuimicos.App.Components.Empleados.TareasAsignadas>();
            var c = captura.Instancias[typeof(global::BanosQuimicos.App.Components.Empleados.TareasAsignadas)];
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            object? Campo(string n) => c.GetType().GetField(n, flags)!.GetValue(c);
            async Task Invocar(string n, params object[] args) { var r = c.GetType().GetMethod(n, flags)!.Invoke(c, args); if (r is Task t) await t; }
            Assert.Equal("Sin realizar", Campo("filtro")); await Invocar("PaginarAsync", 2);
            await Invocar("AbrirAsync", tareas[^1].IdTarea); await Invocar("CargarAsync"); Assert.Equal(2, Campo("pagina"));
            await Invocar("AbrirAsync", tareas[^1].IdTarea); await Invocar("GuardarAsync", false);
            Assert.Equal(11, (await sesion.ResumenTareasAsync()).Cantidad);
            await Invocar("GuardarAsync", true); await Invocar("CargarAsync");
            Assert.Equal(1, Campo("pagina")); Assert.Equal(10, Campo("total"));
            var inicio = await renderer.RenderComponentAsync<global::BanosQuimicos.App.Components.Empleados.InicioEmpleado>();
            Assert.Contains("10 tarea(s) sin finalizar", inicio.ToHtmlString());
            c.GetType().GetField("filtro", flags)!.SetValue(c, "Todas"); await Invocar("FiltrarAsync"); Assert.Equal(1, Campo("pagina"));
            await Invocar("PaginarAsync", 2); await Invocar("AbrirAsync", tareas[^1].IdTarea); await Invocar("CargarAsync");
            Assert.Equal("Todas", Campo("filtro")); Assert.Equal(2, Campo("pagina"));
        });
    }
}
