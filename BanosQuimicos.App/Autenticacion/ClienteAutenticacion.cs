using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BanosQuimicos.App.Autenticacion;

public enum TipoErrorApi { Credenciales, SesionNoValida, SinPermiso, CuentaNoHabilitada, Duplicado, Validacion, Conexion, Servidor }
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
            throw new ErrorApi(TipoErrorApi.Duplicado, "El correo electrónico ya está registrado.");
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
