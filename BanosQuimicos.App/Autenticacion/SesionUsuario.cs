namespace BanosQuimicos.App.Autenticacion;

public enum EstadoSesion { SinSesion, Verificando, Pendiente, Activa, SinVerificar }

public class SesionUsuario(ClienteAutenticacion api, IAlmacenSesion almacen)
{
    private readonly SemaphoreSlim exclusión = new(1, 1);
    private string? token;
    private bool iniciada;
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
        await exclusión.WaitAsync();
        try
        {
            token = null; Cuenta = null; Estado = EstadoSesion.SinSesion; Mensaje = "Se cerró la sesión.";
            try { await almacen.BorrarAsync(); }
            catch { Mensaje = "Se cerró la sesión. No fue posible limpiar el almacén seguro; su restauración quedó deshabilitada."; }
        }
        finally { Notificar(); exclusión.Release(); }
    }
    public Task<PaginaCuentas> ListarCuentasAsync(int pagina, string? busqueda, string? estado) =>
        ComoAdministradorAsync(t => api.ListarCuentasAsync(t, pagina, busqueda, estado));
    public Task<CuentaGestion> DetalleCuentaAsync(int id) => ComoAdministradorAsync(t => api.DetalleCuentaAsync(t, id));
    public Task<bool> ActivarCuentaAsync(int id) => ComoAdministradorAsync(async t => { await api.ActivarCuentaAsync(t, id); return true; });

    private async Task<T> ComoAdministradorAsync<T>(Func<string, Task<T>> operacion)
    {
        await exclusión.WaitAsync();
        try
        {
            if (token is null || Estado != EstadoSesion.Activa || Cuenta?.Rol != "Administrador")
                throw new ErrorApi(TipoErrorApi.SinPermiso, "Solo un administrador activo puede gestionar cuentas.");
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
