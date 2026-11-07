using System.Net;
using System.Net.Http.Json;
using BanosQuimicos.Api.Controllers;
using BanosQuimicos.Api.Modelos;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BanosQuimicos.Api.Pruebas;

public class ServiciosClientePruebas(AplicacionTareasPruebas app) : IClassFixture<AplicacionTareasPruebas>
{
    private readonly TareasEmpleadoPruebas datos = new(app);
    private CancellationToken Ct => TestContext.Current.CancellationToken;
    private record Resumen(int Cantidad);

    private async Task<int> Servicio(int usuario, string estado, string direccion, bool actual = true)
    {
        using var db = app.NuevoContexto();
        var ahora = AplicacionTareasPruebas.Ahora;
        var s = new Servicio {
            IdCliente = await db.Clientes.Where(c => c.IdUsuario == usuario).Select(c => c.IdCliente).SingleAsync(Ct),
            Bano = new BanoQuimico { CodigoInterno = "SC-" + Guid.NewGuid().ToString("N")[..12], IdTipoBano = 1, IdEstadoBano = 1, FechaAlta = ahora },
            TipoUso = "Obra", CantidadLimpiezasSemana = 2, EstadoServicio = estado, FechaAsignacion = ahora.AddDays(-5),
            FechaEntregaReal = estado is "Entregado" or "Finalizado" ? ahora.AddDays(-4) : null,
            FechaRetiroReal = estado == "Finalizado" ? ahora : null,
            FechaFinalizacion = estado == "Finalizado" ? ahora : null,
            FechaCancelacion = estado == "Cancelado" ? ahora : null
        };
        db.UbicacionesServicio.Add(new UbicacionServicio { Servicio = s, Direccion = direccion, EsActual = actual,
            FechaRegistro = ahora, FechaDesde = actual ? ahora : null });
        await db.SaveChangesAsync(Ct); return s.IdServicio;
    }

    [Fact]
    public async Task Dos_clientes_ven_solo_sus_vigentes_y_no_pueden_elegir_otra_identidad()
    {
        var a = await datos.Usuario("Cliente"); var b = await datos.Usuario("Cliente");
        var entregado = await Servicio(a.Id, "Entregado", "PRUEBA ACTUAL A");
        var preparado = await Servicio(a.Id, "Pendiente de entrega", "PRUEBA PREPARADA A", false);
        var finalizado = await Servicio(a.Id, "Finalizado", "PRUEBA HISTORICA A");
        var cancelado = await Servicio(a.Id, "Cancelado", "PRUEBA CANCELADA A", false);
        var ajeno = await Servicio(b.Id, "Entregado", "PRUEBA PRIVADA B");
        using var http = datos.Http(a.Token);
        var lista = await http.GetFromJsonAsync<List<ServicioClienteRespuesta>>($"/api/servicios-cliente/mios?idCliente={b.Id}", Ct);
        Assert.Equal(new[] { entregado, preparado }.Order(), lista!.Select(s => s.IdServicio).Order());
        Assert.Equal(2, (await http.GetFromJsonAsync<Resumen>("/api/servicios-cliente/mios/resumen", Ct))!.Cantidad);
        var detalle = await http.GetFromJsonAsync<ServicioClienteRespuesta>($"/api/servicios-cliente/{preparado}", Ct);
        Assert.Equal("PRUEBA PREPARADA A", detalle!.Direccion); Assert.Equal(2, detalle.CantidadLimpiezasSemana);
        Assert.Equal("Obra", detalle.TipoUso); Assert.False(string.IsNullOrEmpty(detalle.CodigoBano));
        foreach (var id in new[] { ajeno, finalizado, cancelado, int.MaxValue })
            Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync($"/api/servicios-cliente/{id}", Ct)).StatusCode);
        using var otro = datos.Http(b.Token);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"/api/servicios-cliente/{entregado}", Ct)).StatusCode);
        Assert.Equal(ajeno, Assert.Single((await otro.GetFromJsonAsync<List<ServicioClienteRespuesta>>("/api/servicios-cliente/mios", Ct))!).IdServicio);
        // Un retiro ya completado deja de contar sin eliminar el servicio ni su ubicación.
        using var db = app.NuevoContexto();
        var s = await db.Servicios.SingleAsync(s => s.IdServicio == entregado, Ct);
        s.EstadoServicio = "Finalizado"; s.FechaRetiroReal = AplicacionTareasPruebas.Ahora; s.FechaFinalizacion = s.FechaRetiroReal;
        await db.SaveChangesAsync(Ct);
        Assert.Equal(1, (await http.GetFromJsonAsync<Resumen>("/api/servicios-cliente/mios/resumen", Ct))!.Cantidad);
        Assert.True(await db.UbicacionesServicio.AnyAsync(u => u.IdServicio == entregado, Ct));
    }

    [Fact]
    public async Task Cliente_sin_servicios_recibe_cero_y_lista_vacia()
    {
        var c = await datos.Usuario("Cliente"); using var http = datos.Http(c.Token);
        Assert.Empty((await http.GetFromJsonAsync<List<ServicioClienteRespuesta>>("/api/servicios-cliente/mios", Ct))!);
        Assert.Equal(0, (await http.GetFromJsonAsync<Resumen>("/api/servicios-cliente/mios/resumen", Ct))!.Cantidad);
    }

    [Theory]
    [InlineData("SinSesion", 401)]
    [InlineData("Administrador", 403)]
    [InlineData("Empleado", 403)]
    [InlineData("Pendiente", 403)]
    [InlineData("Suspendida", 401)]
    [InlineData("BajaLogica", 401)]
    public async Task Las_tres_rutas_exigen_cliente_activo(string tipo, int codigo)
    {
        string? token = null;
        if (tipo == "Administrador") token = await datos.Token(app.CorreoAdmin, app.ClaveAdmin);
        else if (tipo != "SinSesion")
        {
            var c = await datos.Usuario(tipo == "Empleado" ? "Empleado" : "Cliente", tipo != "Pendiente"); token = c.Token;
            if (tipo is "Suspendida" or "BajaLogica")
            {
                using var db = app.NuevoContexto(); var u = await db.Usuarios.SingleAsync(u => u.IdUsuario == c.Id, Ct);
                u.EstadoCuenta = tipo; await db.SaveChangesAsync(Ct);
            }
        }
        using var http = datos.Http(token);
        foreach (var ruta in new[] { "mios", "mios/resumen", "1" })
            Assert.Equal((HttpStatusCode)codigo, (await http.GetAsync("/api/servicios-cliente/" + ruta, Ct)).StatusCode);
    }
}
