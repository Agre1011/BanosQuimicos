using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BanosQuimicos.App.Autenticacion;

public enum TipoErrorApi { Credenciales, SesionNoValida, SinPermiso, CuentaNoHabilitada, Duplicado, Validacion, Conexion, Servidor, Conflicto, NoEncontrado }
public class ErrorApi(TipoErrorApi tipo, string mensaje) : Exception(mensaje)
{
    public TipoErrorApi Tipo { get; } = tipo;
}

public class ClienteAutenticacion
{
    private readonly HttpClient http;
    public ClienteAutenticacion(HttpClient http)
    {
        if (http.BaseAddress is not { IsAbsoluteUri: true } baseUri ||
            (baseUri.Scheme != "https" && !(baseUri.Scheme == "http" && baseUri.IsLoopback)))
            throw new ArgumentException("La API requiere HTTPS; HTTP se limita al loopback de desarrollo.");
        this.http = http;
    }

    public async Task RegistrarAsync(DatosRegistro datos)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/autenticacion/registro") { Content = JsonContent.Create(datos) };
        using var response = await EnviarAsync(request);
        await ComprobarAsync(response, "registro");
    }
    public async Task<RespuestaLogin> LoginAsync(DatosLogin datos)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/autenticacion/login") { Content = JsonContent.Create(datos) };
        using var response = await EnviarAsync(request);
        await ComprobarAsync(response, "login");
        var login = await LeerAsync<RespuestaLogin>(response);
        if (string.IsNullOrWhiteSpace(login.AccessToken)) throw new ErrorApi(TipoErrorApi.Servidor, "La API no devolvió una sesión válida.");
        return login;
    }
    public async Task<CuentaActual> ConsultarAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/autenticacion/yo");
        // El token se adjunta solo a esta petición de nuestra API, nunca a un cliente global.
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await EnviarAsync(request);
        await ComprobarAsync(response, "yo");
        var cuenta = await LeerAsync<CuentaActual>(response);
        if (cuenta.EstadoCuenta is not ("Pendiente" or "Activa") ||
            (cuenta.EstadoCuenta == "Activa" && (cuenta.IdUsuario is not > 0 || string.IsNullOrWhiteSpace(cuenta.Nombre)
                || cuenta.Rol is not ("Cliente" or "Empleado" or "Administrador"))))
            throw new ErrorApi(TipoErrorApi.Servidor, "No pudimos verificar el estado de la cuenta.");
        return cuenta;
    }
    public async Task<PaginaCuentas> ListarCuentasAsync(string token, int pagina, string? busqueda, string? estado, string? rol = null)
    {
        var ruta = $"api/cuentas?pagina={pagina}&tamanoPagina=10&busqueda={Uri.EscapeDataString(busqueda?.Trim() ?? "")}";
        if (!string.IsNullOrEmpty(estado)) ruta += "&estado=" + Uri.EscapeDataString(estado);
        if (!string.IsNullOrEmpty(rol)) ruta += "&rol=" + Uri.EscapeDataString(rol);
        using var request = PeticionCuenta(HttpMethod.Get, ruta, token);
        using var response = await EnviarAsync(request);
        await ComprobarAsync(response, "cuentas");
        return await LeerAsync<PaginaCuentas>(response);
    }
    public async Task<CuentaGestion> DetalleCuentaAsync(string token, int id)
    {
        using var request = PeticionCuenta(HttpMethod.Get, $"api/cuentas/{id}", token);
        using var response = await EnviarAsync(request);
        await ComprobarAsync(response, "cuentas");
        return await LeerAsync<CuentaGestion>(response);
    }
    public async Task ActivarCuentaAsync(string token, int id)
    {
        using var request = PeticionCuenta(HttpMethod.Post, $"api/cuentas/{id}/activar", token);
        using var response = await EnviarAsync(request);
        await ComprobarAsync(response, "cuentas");
    }
    public async Task CambiarRolAsync(string token, int id, string rolAnterior, string nuevoRol)
    {
        using var request = PeticionCuenta(HttpMethod.Put, $"api/cuentas/{id}/rol", token);
        request.Content = JsonContent.Create(new { rolAnterior, nuevoRol });
        using var response = await EnviarAsync(request);
        await ComprobarAsync(response, "rol");
    }
    public Task<ResumenTareas> ResumenTareasAsync(string token) => LeerAutenticadoAsync<ResumenTareas>(token, "api/tareas/mias/resumen");
    public Task<List<EstadoBano>> EstadoBanosAsync(string token) => LeerAutenticadoAsync<List<EstadoBano>>(token, "api/banos/estado", "banos");
    public Task<List<SolicitudResumen>> SolicitudesAsync(string token) => LeerAutenticadoAsync<List<SolicitudResumen>>(token, "api/solicitudes", "solicitudes");
    public Task<SolicitudDetalle> DetalleSolicitudAsync(string token, int id) => LeerAutenticadoAsync<SolicitudDetalle>(token, $"api/solicitudes/{id}", "solicitudes");
    public Task<List<BanoDisponible>> BanosDisponiblesAsync(string token, int id) => LeerAutenticadoAsync<List<BanoDisponible>>(token, $"api/solicitudes/{id}/banos-disponibles", "solicitudes");
    public async Task<SolicitudDetalle> AsignarBanosAsync(string token, int id, AsignacionBanos datos)
    {
        using var request = PeticionCuenta(HttpMethod.Post, $"api/solicitudes/{id}/asignar-banos", token);
        request.Content = JsonContent.Create(datos);
        using var response = await EnviarAsync(request); await ComprobarAsync(response, "solicitudes");
        return await LeerAsync<SolicitudDetalle>(response);
    }
    public async Task<SolicitudDetalle> AsignarSolicitudAsync(string token, int id, AsignacionSolicitud datos)
    {
        using var request = PeticionCuenta(HttpMethod.Post, $"api/solicitudes/{id}/asignar", token);
        request.Content = JsonContent.Create(datos);
        using var response = await EnviarAsync(request); await ComprobarAsync(response, "solicitudes");
        return await LeerAsync<SolicitudDetalle>(response);
    }
    public Task<List<EmpleadoAsignable>> EmpleadosAsignablesAsync(string token) => LeerAutenticadoAsync<List<EmpleadoAsignable>>(token, "api/banos/empleados-asignables", "banos");
    public async Task<EstadoBano> AsignarResponsableAsync(string token, int id, int empleado, int? anterior)
    {
        using var request = PeticionCuenta(HttpMethod.Put, $"api/banos/{id}/responsable-limpieza", token);
        request.Content = JsonContent.Create(new { idEmpleado = empleado, idResponsableAnterior = anterior });
        using var response = await EnviarAsync(request); await ComprobarAsync(response, "banos");
        return await LeerAsync<EstadoBano>(response);
    }
    public async Task<LimpiezaProgramada> ProgramarLimpiezaAsync(string token, int id, ProgramarLimpieza datos)
    {
        using var request = PeticionCuenta(HttpMethod.Post, $"api/banos/{id}/limpiezas", token);
        request.Content = JsonContent.Create(datos);
        using var response = await EnviarAsync(request); await ComprobarAsync(response, "banos");
        return await LeerAsync<LimpiezaProgramada>(response);
    }
    public Task<List<TareaAsignada>> TareasAsync(string token) => LeerAutenticadoAsync<List<TareaAsignada>>(token, "api/tareas/mias");
    public Task<TareaAsignada> DetalleTareaAsync(string token, int id) => LeerAutenticadoAsync<TareaAsignada>(token, $"api/tareas/{id}");
    public async Task<TareaAsignada> GuardarTrabajoAsync(string token, int id, bool finalizar)
    {
        using var request = PeticionCuenta(HttpMethod.Post, $"api/tareas/{id}/{(finalizar ? "finalizar" : "registrar-trabajo")}", token);
        using var response = await EnviarAsync(request);
        await ComprobarAsync(response, "tareas");
        return await LeerAsync<TareaAsignada>(response);
    }
    private async Task<T> LeerAutenticadoAsync<T>(string token, string ruta, string operacion = "tareas")
    {
        using var request = PeticionCuenta(HttpMethod.Get, ruta, token);
        using var response = await EnviarAsync(request);
        await ComprobarAsync(response, operacion);
        return await LeerAsync<T>(response);
    }
    private static HttpRequestMessage PeticionCuenta(HttpMethod metodo, string ruta, string token)
    {
        var request = new HttpRequestMessage(metodo, ruta);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
    private async Task<HttpResponseMessage> EnviarAsync(HttpRequestMessage request)
    {
        // Las rutas son internas y fijas. Un URI absoluto o un cambio de servidor no recibe credenciales.
        var destino = new Uri(http.BaseAddress!, request.RequestUri!);
        if (!http.BaseAddress!.IsBaseOf(destino)) throw new InvalidOperationException("Destino fuera de nuestra API.");
        try { return await http.SendAsync(request); }
        catch (HttpRequestException) { throw new ErrorApi(TipoErrorApi.Conexion, "No pudimos conectar con el servidor. Revisá la conexión y que la API esté ejecutándose."); }
        catch (TaskCanceledException) { throw new ErrorApi(TipoErrorApi.Conexion, "El servidor tardó demasiado. Podés volver a intentar."); }
    }
    private static async Task<T> LeerAsync<T>(HttpResponseMessage response)
    {
        try { return await response.Content.ReadFromJsonAsync<T>() ?? throw new JsonException(); }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        { throw new ErrorApi(TipoErrorApi.Servidor, "El servidor devolvió una respuesta que no pudimos interpretar."); }
    }
    private static async Task ComprobarAsync(HttpResponseMessage response, string operacion)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new ErrorApi(operacion == "login" ? TipoErrorApi.Credenciales : TipoErrorApi.SesionNoValida,
                operacion == "login" ? "Correo o contraseña incorrectos." : "La sesión venció o la cuenta ya no está habilitada. Iniciá sesión nuevamente.");
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new ErrorApi(operacion == "login" ? TipoErrorApi.CuentaNoHabilitada : TipoErrorApi.SinPermiso,
                operacion == "login" ? "La cuenta no está habilitada. Consultá al administrador." : "No tenés permiso para realizar esta operación.");
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            if (operacion == "solicitudes") throw new ErrorApi(TipoErrorApi.Conflicto, "La solicitud ya tiene servicios o trabajos, cambió su estado o algún baño o empleado no permite la asignación. Consultá el detalle y la disponibilidad antes de continuar.");
            if (operacion == "banos") throw new ErrorApi(TipoErrorApi.Conflicto, "El baño, servicio o empleado cambió, o la programación no es válida. Consultá nuevamente antes de intentar otra operación.");
            throw new ErrorApi(operacion == "registro" ? TipoErrorApi.Duplicado : TipoErrorApi.Conflicto,
                operacion == "registro" ? "El correo electrónico ya está registrado." : operacion == "tareas" ? "La tarea cambió o no reúne las condiciones para esta acción. Volvé a consultar el detalle; si fue reasignada o cancelada, consultá al administrador." : "La cuenta cambió o no cumple las condiciones para esta operación.");
        }
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            if (operacion == "solicitudes") throw new ErrorApi(TipoErrorApi.NoEncontrado, "La solicitud no está disponible.");
            throw new ErrorApi(TipoErrorApi.NoEncontrado, operacion == "banos" ? "El baño no está disponible." : operacion == "tareas" ? "La tarea no está disponible." : "La cuenta ya no está disponible.");
        }
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var mensaje = "Revisá los datos ingresados y los campos obligatorios.";
            try
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.TryGetProperty("errors", out var errores) && errores.ValueKind == JsonValueKind.Object)
                {
                    var campos = errores.EnumerateObject().Select(x => x.Name).Where(x =>
                        x is "Nombre" or "Apellido" or "Email" or "Telefono" or "Contrasena" or "RazonSocial" or "Cuit" or "DireccionPrincipal");
                    var lista = string.Join(", ", campos);
                    if (lista.Length > 0) mensaje += " Campos a revisar: " + lista + ".";
                }
            }
            catch (JsonException) { }
            throw new ErrorApi(TipoErrorApi.Validacion, mensaje);
        }
        throw new ErrorApi(TipoErrorApi.Servidor, "No pudimos completar la solicitud. Intentá nuevamente más tarde.");
    }
}
