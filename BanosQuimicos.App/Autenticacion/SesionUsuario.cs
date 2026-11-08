namespace BanosQuimicos.App.Autenticacion;

public enum EstadoSesion { SinSesion, Verificando, Pendiente, Activa, SinVerificar }

public class SesionUsuario(ClienteAutenticacion api, IAlmacenSesion almacen)
{
    private readonly SemaphoreSlim exclusión = new(1, 1);
    private string? token;
    private bool iniciada;
    private DatosNuevoServicio? borradorServicio;
    private int? duenoBorrador;
    private readonly Dictionary<(int Usuario, int Servicio, bool Reclamo), DatosSolicitudServicio> borradoresOperacion = [];
    private readonly Dictionary<(int Usuario, int Servicio), DatosDireccionCliente> direcciones = [];
    private readonly Dictionary<(int Usuario, int Servicio), DatosBajaCliente> bajas = [];
    private (int, int) ClaveBorrador(int servicio) => Estado == EstadoSesion.Activa && Cuenta?.Rol == "Cliente" && Cuenta.IdUsuario is int usuario
        ? (usuario, servicio) : throw new ErrorApi(TipoErrorApi.SinPermiso, "Se requiere un cliente activo.");
    public DatosDireccionCliente BorradorDireccion(int servicio, int ubicacion)
    {
        var clave = ClaveBorrador(servicio);
        if (!direcciones.TryGetValue(clave, out var d)) direcciones[clave] = d = new() { IdUbicacionEsperada = ubicacion };
        return d;
    }
    public DatosBajaCliente BorradorBaja(int servicio)
    {
        var clave = ClaveBorrador(servicio);
        if (!bajas.TryGetValue(clave, out var d)) bajas[clave] = d = new(); return d;
    }
    public void LimpiarDireccionBaja(int servicio, bool baja)
    {
        var clave = ClaveBorrador(servicio); if (baja) bajas.Remove(clave); else direcciones.Remove(clave);
    }
    public DatosSolicitudServicio BorradorOperacion(int servicio, bool reclamo)
    {
        if (Estado != EstadoSesion.Activa || Cuenta?.Rol != "Cliente" || Cuenta.IdUsuario is not int usuario)
            throw new ErrorApi(TipoErrorApi.SinPermiso, "Se requiere un cliente activo.");
        var clave = (usuario, servicio, reclamo);
        if (!borradoresOperacion.TryGetValue(clave, out var datos)) borradoresOperacion[clave] = datos = new();
        return datos;
    }
    public void LimpiarBorradorOperacion(int servicio, bool reclamo)
    {
        if (Cuenta?.IdUsuario is int usuario) borradoresOperacion.Remove((usuario, servicio, reclamo));
    }
    public DatosNuevoServicio BorradorServicio()
    {
        if (Estado != EstadoSesion.Activa || Cuenta?.Rol != "Cliente")
            throw new ErrorApi(TipoErrorApi.SinPermiso, "Se requiere un cliente activo.");
        if (duenoBorrador != Cuenta.IdUsuario) { borradorServicio = null; duenoBorrador = Cuenta.IdUsuario; }
        return borradorServicio ??= new DatosNuevoServicio();
    }
    public void NuevoBorradorServicio() { borradorServicio = null; }
    public EstadoSesion Estado { get; private set; } = EstadoSesion.Verificando;
    public CuentaActual? Cuenta { get; private set; }
    public string? Mensaje { get; private set; }
    public bool Ocupada { get; private set; }
    public event Action? Cambio;

    public async Task InicializarAsync()
    {
        if (!await exclusión.WaitAsync(0)) return;
        try
        {
            if (iniciada) return;
            iniciada = true; Ocupada = true; Notificar();
            try { token = await almacen.LeerAsync(); }
            catch
            {
                token = null; Mensaje = "No pudimos restaurar la sesión guardada. Iniciá sesión nuevamente.";
                try { await almacen.BorrarAsync(); } catch { }
            }
            if (string.IsNullOrEmpty(token)) Estado = EstadoSesion.SinSesion;
            else await ConsultarInternoAsync();
        }
        finally { Ocupada = false; Notificar(); exclusión.Release(); }
    }
    public async Task IngresarAsync(DatosLogin datos)
    {
        if (!await exclusión.WaitAsync(0)) return;
        try
        {
            iniciada = true; Ocupada = true; Mensaje = null; Notificar();
            var respuesta = await api.LoginAsync(datos);
            token = respuesta.AccessToken;
            Cuenta = null;
            // El rol/estado siempre procede de /yo, no de datos guardados ni del JWT decodificado.
            await ConsultarInternoAsync();
            if (token is not null)
            {
                try { await almacen.GuardarAsync(token); }
                catch { Mensaje = "No pudimos guardar la sesión de forma segura. Se conservará solo mientras la app esté abierta."; }
            }
        }
        finally { Ocupada = false; Notificar(); exclusión.Release(); }
    }
    public async Task ActualizarAsync()
    {
        if (!await exclusión.WaitAsync(0)) return;
        try
        {
            if (token is null) return;
            Ocupada = true; Mensaje = null;
            await ConsultarInternoAsync();
        }
        finally { Ocupada = false; Notificar(); exclusión.Release(); }
    }
    private async Task ConsultarInternoAsync()
    {
        Estado = EstadoSesion.Verificando; Cuenta = null; Notificar();
        try
        {
            var cuenta = await api.ConsultarAsync(token!);
            if (cuenta.EstadoCuenta == "Pendiente")
            {
                // Una cuenta pendiente no conserva datos de perfil ni rol aunque llegaran por error.
                Cuenta = new CuentaActual { EstadoCuenta = "Pendiente" };
                Estado = EstadoSesion.Pendiente;
            }
            else { Cuenta = cuenta; Estado = EstadoSesion.Activa; }
        }
        catch (ErrorApi ex)
        {
            Mensaje = ex.Message;
            if (ex.Tipo == TipoErrorApi.SesionNoValida)
            {
                token = null; Estado = EstadoSesion.SinSesion;
                try { await almacen.BorrarAsync(); } catch { }
            }
            else Estado = EstadoSesion.SinVerificar; // Sin red o 403 no se muestra una identidad activa sin verificar.
        }
    }
    public async Task CerrarAsync()
    {
        inventario.Clear();
        await exclusión.WaitAsync();
        try
        {
            token = null; Cuenta = null; Estado = EstadoSesion.SinSesion; Mensaje = "Se cerró la sesión.";
            borradorServicio = null; duenoBorrador = null;
            borradoresOperacion.Clear();
            direcciones.Clear(); bajas.Clear();
            try { await almacen.BorrarAsync(); }
            catch { Mensaje = "Se cerró la sesión. No fue posible limpiar el almacén seguro; su restauración quedó deshabilitada."; }
        }
        finally { Notificar(); exclusión.Release(); }
    }
    public Task<PaginaCuentas> ListarCuentasAsync(int pagina, string? busqueda, string? estado, string? rol = null) =>
        ComoAdministradorAsync(t => api.ListarCuentasAsync(t, pagina, busqueda, estado, rol));
    public Task<bool> CambiarRolAsync(int id, string rolAnterior, string nuevoRol) =>
        ComoAdministradorAsync(async t => { await api.CambiarRolAsync(t, id, rolAnterior, nuevoRol); return true; });
    public Task<CuentaGestion> DetalleCuentaAsync(int id) => ComoAdministradorAsync(t => api.DetalleCuentaAsync(t, id));
    public Task<bool> CambiarEstadoCuentaAsync(int id, string anterior, string nuevo, string? motivo) => ComoAdministradorAsync(async t => { await api.CambiarEstadoCuentaAsync(t, id, anterior, nuevo, motivo); return true; });
    public Task<bool> ActivarCuentaAsync(int id) => ComoAdministradorAsync(async t => { await api.ActivarCuentaAsync(t, id); return true; });

    public Task<ResumenTareas> ResumenTareasAsync() => ComoRolAsync("Empleado", api.ResumenTareasAsync);
    public Task<ResumenServiciosCliente> ResumenServiciosClienteAsync() => ComoRolAsync("Cliente", api.ResumenServiciosClienteAsync);
    public Task<List<SolicitudPropia>> MisSolicitudesAsync() => ComoRolAsync("Cliente", api.MisSolicitudesAsync);
    public Task<SolicitudPropia> CambiarDireccionAsync(int id, DatosDireccionCliente d) => ComoRolAsync("Cliente", t => api.CambiarDireccionAsync(t, id, d));
    public Task<SolicitudPropia> PedirBajaAsync(int id, DatosBajaCliente d) => ComoRolAsync("Cliente", t => api.PedirBajaAsync(t, id, d));
    public Task<RevisionDireccion> RevisionDireccionAsync(int id) => ComoAdministradorAsync(t => api.RevisionDireccionAsync(t, id));
    public Task<RevisionDireccion> RevisarDireccionAsync(int id, RevisarDireccion d) => ComoAdministradorAsync(t => api.RevisarDireccionAsync(t, id, d));
    public Task<SolicitudPropia> SolicitarSobreServicioAsync(int id, bool reclamo, DatosSolicitudServicio datos) =>
        ComoRolAsync("Cliente", t => api.SolicitarSobreServicioAsync(t, id, reclamo, datos));
    public Task<List<TipoBanoPedido>> TiposBanoPedidoAsync() => ComoRolAsync("Cliente", api.TiposBanoPedidoAsync);
    public Task<SolicitudPropia> SolicitarServicioAsync(DatosNuevoServicio datos) => ComoRolAsync("Cliente", t => api.SolicitarServicioAsync(t, datos));
    public Task<List<ServicioCliente>> MisServiciosAsync() => ComoRolAsync("Cliente", api.MisServiciosAsync);
    public Task<ServicioCliente> MiServicioAsync(int id) => ComoRolAsync("Cliente", t => api.MiServicioAsync(t, id));
    public Task<List<EstadoBano>> EstadoBanosAsync(string filtro = "Activos", string condicion = "Todos") => ComoAdministradorAsync(t => api.EstadoBanosAsync(t, filtro, condicion));
    private readonly Dictionary<int, BorradorInventario> inventario = [];
    public BorradorInventario InventarioActual()
    {
        if (Estado != EstadoSesion.Activa || Cuenta?.Rol != "Administrador" || Cuenta.IdUsuario is not int usuario)
            throw new ErrorApi(TipoErrorApi.SinPermiso, "Se requiere un administrador activo.");
        if (!inventario.TryGetValue(usuario, out var b)) inventario[usuario] = b = new(); return b;
    }
    public Task<List<TipoBanoPedido>> TiposInventarioAsync() => ComoAdministradorAsync(api.TiposInventarioAsync);
    public Task<EstadoBano> AltaInventarioAsync(AltaInventario d) => ComoAdministradorAsync(t => api.InventarioAsync<EstadoBano>(t, "api/banos", HttpMethod.Post, d));
    public Task<EstadoBano> CondicionInventarioAsync(int id, CondicionInventario d) => ComoAdministradorAsync(t => api.InventarioAsync<EstadoBano>(t, $"api/banos/{id}/condicion", HttpMethod.Put, d));
    public Task<EstadoBano> BajaInventarioAsync(int id) => ComoAdministradorAsync(t => api.InventarioAsync<EstadoBano>(t, $"api/banos/{id}/baja-inventario", HttpMethod.Post));
    public Task<LimpiezaProgramada> MantenimientoInventarioAsync(int id, MantenimientoInventario d) => ComoAdministradorAsync(t => api.InventarioAsync<LimpiezaProgramada>(t, $"api/banos/{id}/mantenimientos", HttpMethod.Post, d));
    public Task<List<ServicioGestion>> ServiciosAsync() => ComoAdministradorAsync(api.ServiciosAsync);
    public Task<ServicioGestion> ProgramarRetiroAsync(int id, ProgramarRetiro datos) => ComoAdministradorAsync(t => api.ProgramarRetiroAsync(t, id, datos));
    public Task<ServicioGestion> ResolverPendientesAsync(int id, ResolverPendientes datos) => ComoAdministradorAsync(t => api.ResolverPendientesAsync(t, id, datos));
    public Task<List<SolicitudResumen>> SolicitudesAsync() => ComoAdministradorAsync(api.SolicitudesAsync);
    public Task<SolicitudDetalle> DetalleSolicitudAsync(int id) => ComoAdministradorAsync(t => api.DetalleSolicitudAsync(t, id));
    public Task<List<BanoDisponible>> BanosDisponiblesAsync(int id) => ComoAdministradorAsync(t => api.BanosDisponiblesAsync(t, id));
    public Task<SolicitudDetalle> AsignarBanosAsync(int id, AsignacionBanos datos) => ComoAdministradorAsync(t => api.AsignarBanosAsync(t, id, datos));
    public Task<SolicitudDetalle> AsignarSolicitudAsync(int id, AsignacionSolicitud datos) => ComoAdministradorAsync(t => api.AsignarSolicitudAsync(t, id, datos));
    public Task<List<EmpleadoAsignable>> EmpleadosAsignablesAsync() => ComoAdministradorAsync(api.EmpleadosAsignablesAsync);
    public Task<EstadoBano> AsignarResponsableAsync(int id, int empleado, int? anterior) => ComoAdministradorAsync(t => api.AsignarResponsableAsync(t, id, empleado, anterior));
    public Task<LimpiezaProgramada> ProgramarLimpiezaAsync(int id, ProgramarLimpieza datos) => ComoAdministradorAsync(t => api.ProgramarLimpiezaAsync(t, id, datos));
    public Task<List<TareaAsignada>> TareasAsync() => ComoRolAsync("Empleado", api.TareasAsync);
    public Task<TareaAsignada> DetalleTareaAsync(int id) => ComoRolAsync("Empleado", t => api.DetalleTareaAsync(t, id));
    public Task<TareaAsignada> GuardarTrabajoAsync(int id, bool finalizar) => ComoRolAsync("Empleado", t => api.GuardarTrabajoAsync(t, id, finalizar));
    private Task<T> ComoAdministradorAsync<T>(Func<string, Task<T>> operacion) => ComoRolAsync("Administrador", operacion);
    private async Task<T> ComoRolAsync<T>(string rol, Func<string, Task<T>> operacion)
    {
        await exclusión.WaitAsync();
        try
        {
            if (token is null || Estado != EstadoSesion.Activa || Cuenta?.Rol != rol)
                throw new ErrorApi(TipoErrorApi.SinPermiso, $"Se requiere una cuenta activa con rol {rol}.");
            Ocupada = true; Notificar();
            return await operacion(token);
        }
        catch (ErrorApi ex) when (ex.Tipo is TipoErrorApi.SesionNoValida or TipoErrorApi.SinPermiso or TipoErrorApi.Conexion)
        {
            // No dejar datos administrativos visibles cuando ya no se puede verificar el acceso.
            Cuenta = null; Mensaje = ex.Message;
            Estado = ex.Tipo == TipoErrorApi.SesionNoValida ? EstadoSesion.SinSesion : EstadoSesion.SinVerificar;
            if (ex.Tipo == TipoErrorApi.SesionNoValida)
            {
                token = null;
                try { await almacen.BorrarAsync(); } catch { }
            }
            throw;
        }
        finally { Ocupada = false; Notificar(); exclusión.Release(); }
    }
    private void Notificar() => Cambio?.Invoke();
}
