namespace BanosQuimicos.App.Plataforma;

public static class ConexionApi
{
    public static HttpClient CrearCliente()
    {
        var direccion = "https://localhost:7292/";
#if DEBUG && ANDROID
        // Solo Debug, loopback del teléfono redirigido a la PC mediante adb reverse por USB.
        direccion = "http://127.0.0.1:5148/";
#endif
        // No seguir redirecciones: una respuesta 3xx no debe reenviar credenciales fuera de la API.
        return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        { BaseAddress = new Uri(direccion), Timeout = TimeSpan.FromSeconds(20) };
    }
}
